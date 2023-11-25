using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerSearcher.Utility
{
    internal static class DatabaseHelper
    {
        public static async Task<List<string>> GetAllTableNames(SqlConnection connection, string dbName)
        {
            var query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = @tabletype AND TABLE_CATALOG=@db";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@tabletype", "BASE TABLE");
            command.Parameters.AddWithValue("@db", dbName);

            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            var tables = new List<string>();
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                tables.Add(reader[0].ToString() ?? string.Empty);
            }

            return tables;
        }
    }
}
