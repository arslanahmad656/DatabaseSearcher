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
        : base (new SqlConnection(connectionString))
    {
        
    }

    public override async Task<List<string>> GetTableNames(CancellationToken cancellationToken)
    {
        var query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = @tabletype AND TABLE_CATALOG=@db";

        var parameters = new List<(string name, object value)>
        {
            ("tabletype", "BASE TABLE"),
            ("db", this.DatabaseName)
        };

        var reader = await this.GetReader(query, parameters, cancellationToken).ConfigureAwait(false);
        var rows = new List<Dictionary<string, object>>();
        await foreach (var row in DatabaseExtensions.ReadTableRows(reader, null, cancellationToken).ConfigureAwait(false))
        {
            rows.Add(row);
        }

        var tableNames = rows.Select(r => r["TABLE_NAME"]?.ToString() ?? string.Empty).ToList();
        return tableNames;
    }
}
