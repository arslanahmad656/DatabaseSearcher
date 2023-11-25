using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerSearcher.Utility
{
    public static class DatabaseExtensions
    {
        public static Dictionary<string, object> ReadTableRow(this DbDataReader reader)
        {
            var row = new Dictionary<string, object>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }

            return row;
        }

        public static async IAsyncEnumerable<Dictionary<string, object>> ReadTableRows(this DbDataReader reader, IProgress<int>? progress, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var totalRowsProcessed = 0;

            progress?.Report(totalRowsProcessed);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (totalRowsProcessed % 10 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var row = reader.ReadTableRow();
                totalRowsProcessed++;
                progress?.Report(totalRowsProcessed);
                yield return row;
            }
        }
    }
}
