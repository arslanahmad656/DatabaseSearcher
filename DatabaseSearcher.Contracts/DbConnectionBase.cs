using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseSearcher.Contracts;

public abstract class DbConnectionBase : IDisposable, IAsyncDisposable
{
    private readonly DbConnection _connection;
    private readonly bool _disposeConnection;

    public string ConnectionString => _connection.ConnectionString;

    public string DatabaseName => _connection.Database;

    public bool Connected => _connection.State == ConnectionState.Open;

    public bool Disposed { get; private set; }

    public DbConnectionBase(DbConnection connection)
        : this(connection, true)
    {
    }

    public DbConnectionBase(DbConnection connection, bool disposeConnection)
    {
        _connection = connection;
        _disposeConnection = disposeConnection;
    }

    public virtual async Task Connect(CancellationToken cancellationToken)
    {
        await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task Disconnect()
    {
        await _connection.CloseAsync().ConfigureAwait(false);
    }

    public virtual async Task<DbDataReader> GetReader(string query, ICollection<(string name, object value)>? parameters, CancellationToken cancellationToken)
    {
        await Prepare(cancellationToken).ConfigureAwait(false);
        using var command = CreateCommand(query, parameters);
        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return reader;
    }

    public virtual async Task<object?> GetScalar(string query, ICollection<(string name, object value)>? parameters, CancellationToken cancellationToken)
    {
        await Prepare(cancellationToken).ConfigureAwait(false);
        using var command = CreateCommand(query, parameters);
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value;
    }

    public abstract Task<List<(string tableName, string tableSchema)>> GetTableNames(CancellationToken cancellationToken);

    #region Helpers

    public DbCommand CreateCommand(string query, ICollection<(string name, object value)>? parameters)
    {
        var command = _connection.CreateCommand();
        command.CommandText = query;
        if (parameters is not null)
        {
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                (parameter.ParameterName, parameter.Value) = (name, value);
                command.Parameters.Add(parameter);
            }
        }

        return command;
    }

    protected async Task Prepare(CancellationToken cancellationToken)
    {
        if (!Connected)
        {
            await this.Connect(cancellationToken);
        }
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        if (!_disposeConnection)
        {
            return;
        }

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposeConnection)
        {
            return;
        }

        if (disposing)
        {
            if (!Disposed)
            {
                _connection.Dispose();
                Disposed = true;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposeConnection)
        {
            return;
        }

        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposeConnection)
        {
            return;
        }

        if (!Disposed)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            Disposed = true;
        }
    }

    #endregion
}
