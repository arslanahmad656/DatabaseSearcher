using DatabaseSearcher.Contracts;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLServerSearcher.Utility;
using System.Data;

namespace SQLServerSearcher;

public class SQLServerConnector : DbConnectionBase
{

    public SQLServerConnector(string connectionString)
        : base(new SqlConnection(connectionString))
    {

    }

    public override async Task<List<(string tableName, string tableSchema)>> GetTableNames(CancellationToken cancellationToken)
    {
        await Prepare(cancellationToken).ConfigureAwait(false);
        var query = "SELECT TABLE_NAME, TABLE_SCHEMA FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_CATALOG=@db";   // all tables and views

        var parameters = new List<(string name, object value)>
        {
            ("db", this.DatabaseName)
        };

        using var reader = await this.GetReader(query, parameters, cancellationToken).ConfigureAwait(false);
        var rows = new List<Dictionary<string, object>>();
        await foreach (var row in reader.ReadTableRows(null, cancellationToken).ConfigureAwait(false))
        {
            rows.Add(row);
        }

        var tableNames = rows.Select(r => (r["TABLE_NAME"]?.ToString() ?? string.Empty, r["TABLE_SCHEMA"]?.ToString() ?? string.Empty)).ToList();
        return tableNames;
    }
}
