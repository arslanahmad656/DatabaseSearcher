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
using System.Threading;
using System.Threading.Tasks;

namespace SQLServerSearcher;

public class SQLServerSearcher(string connectionString) : IDbSearcher, IDisposable, IAsyncDisposable
{
    private readonly SQLServerConnector _connector = new(connectionString);

    public bool Disposed { get; private set; }

    public async IAsyncEnumerable<SearchResult> Search(string text, IProgress<Status>? progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fullTableNames = await DbHelper.GetTableNames(_connector, true, cancellationToken).ConfigureAwait(false);

        await foreach (var result in Search(text, fullTableNames, progress, cancellationToken))
        {
            yield return result;
        }
    }

    public async IAsyncEnumerable<SearchResult> Search(string text, ICollection<string> tables, IProgress<Status>? progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queries = tables.Select(t => ($"SELECT COUNT(*) FROM {t}", $"SELECT * FROM {t}", t)).ToList();
        await foreach (var result in DbHelper.Search(_connector, text, queries, progress, cancellationToken).ConfigureAwait(false))
        {
            yield return result;
        }
    }

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

file static class Helper
{
    public static bool IsMatch(string? textToMatch, string? textInDb)
        => string.Equals(textToMatch, textInDb, StringComparison.InvariantCultureIgnoreCase);
}

file static class DbHelper
{
    public static async Task<List<string>> GetTableNames(SQLServerConnector connector, bool getFullNames, CancellationToken cancellationToken)
    {
        var tableInfos = await connector.GetTableNames(cancellationToken).ConfigureAwait(false);
        List<string> tableNames;
        if (getFullNames)
        {
            tableNames = tableInfos.Select(ns => $"[{ns.tableSchema}].[{ns.tableName}]").ToList();
        }
        else
        {
            tableNames = tableInfos.Select(ns => ns.tableName).ToList();
        }

        return tableNames;
    }

    public static async IAsyncEnumerable<SearchResult> Search(SQLServerConnector connector, string text, ICollection<(string countQuery, string tableQuery, string tableName)> tables, IProgress<Status>? progress, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < tables.Count; i++)
        {
            var tableInfo = tables.ElementAt(i);
            var table = tableInfo.tableName;
            cancellationToken.ThrowIfCancellationRequested();

            var countQuery = tableInfo.countQuery;
            var totalRows = Convert.ToInt32(await connector.GetScalar(countQuery, null, cancellationToken).ConfigureAwait(false));

            cancellationToken.ThrowIfCancellationRequested();

            var tableQuery = tableInfo.tableQuery;
            using var reader = await connector.GetReader(tableQuery, null, cancellationToken).ConfigureAwait(false);

            var progressReporter = new Progress<int>(currentTableRowsProcessed =>
            {
                progress?.Report(new((double)i / tables.Count * 100, new(tables.Count, i + 1), new(table, totalRows, currentTableRowsProcessed)));
            });

            await foreach (var cellLocation in Search(connector, text, tableQuery, progressReporter, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new(table, cellLocation);
            }
        }
    }

    private static async IAsyncEnumerable<CellLocation> Search(SQLServerConnector connector, string text, string tableQuery, IProgress<int> progressReporter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = await connector.GetReader(tableQuery, null, cancellationToken).ConfigureAwait(false);

        int rowCount = 0;
        await foreach (var rowData in reader.ReadTableRows(progressReporter, cancellationToken).ConfigureAwait(false))
        {
            rowCount++;
            for (int colCount = 0; colCount < rowData.Keys.Count; colCount++)
            {
                var key = rowData.Keys.ElementAt(colCount);
                var value = rowData[key];
                var columnText = value?.ToString();
                if (Helper.IsMatch(text, columnText))
                {
                    yield return new(key, rowCount);
                }
            }
        }
    }
}