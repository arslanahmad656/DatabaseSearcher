using DatabaseSearcher.Contracts;
using DatabaseSearcher.Dto;
using DatabaseSearcher.Dto.Status;
using SQLServerSearcher.Utility;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerSearcher;

public class SQLServerSearcher : IDbSearcher, IDisposable, IAsyncDisposable
{
    private readonly SQLServerConnector _connector;
    private readonly string _databaseName;

    public bool Disposed { get; private set; }

    public SQLServerSearcher(string connectionString, string databaseName)
    {
        _connector = new(connectionString);
        _databaseName = databaseName;
    }

    public async IAsyncEnumerable<SearchResult> Search(string text, IProgress<Status>? progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var tableNames = await _connector.GetTableNames(cancellationToken).ConfigureAwait(false);

        await foreach (var result in Search(text, tableNames, progress, cancellationToken))
        {
            yield return result;
        }
    }

    public async IAsyncEnumerable<SearchResult> Search(string text, ICollection<string> tables, IProgress<Status>? progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < tables.Count; i++)
        {
            var table = tables.ElementAt(i);
            cancellationToken.ThrowIfCancellationRequested();

            var countQuery = $"SELECT COUNT(*) FROM {table}";
            var totalRows = Convert.ToInt32(await _connector.GetScalar(countQuery, null, cancellationToken).ConfigureAwait(false));

            cancellationToken.ThrowIfCancellationRequested();

            var tableQuery = $"SELECT * FROM {table}";
            var reader = await _connector.GetReader(tableQuery, null, cancellationToken).ConfigureAwait(false);

            var progressReporter = new Progress<int>(currentTableRowsProcessed =>
            {
                progress?.Report(new((double)i / tables.Count, new(tables.Count, currentTableRowsProcessed), new(table, totalRows, currentTableRowsProcessed)));
            });

            await foreach (var tableData in reader.ReadTableRows(progressReporter, cancellationToken).ConfigureAwait(false))
            {
                for (int rowCount = 0; rowCount < tableData.Keys.Count; rowCount++)
                {
                    var key = tableData.Keys.ElementAt(rowCount);
                    var value = tableData[key];
                    var columnText = value?.ToString();
                    if (IsMatch(text, columnText))
                    {
                        yield return new (table, key, rowCount);
                    }
                }
            }
        }
    }

    #region Helpers

    private static bool IsMatch(string? textToMatch, string? textInDb)
        => string.Equals(textToMatch, textInDb, StringComparison.InvariantCultureIgnoreCase);

    #endregion

    #region Dispose

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!Disposed)
            {
                _connector.Dispose();
                Disposed = true;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!Disposed)
        {
            await _connector.DisposeAsync().ConfigureAwait(false);
            Disposed = true;
        }
    }

    #endregion
}
