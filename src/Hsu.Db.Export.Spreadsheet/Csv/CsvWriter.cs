using System.Text.RegularExpressions;

namespace Hsu.Db.Export.Spreadsheet.Csv;

public static class CsvWriter
{
    public static int Write(IEnumerable<object> rows, StreamWriter writer, ExportColumn[] columns, string separator, CancellationToken? cancellation = default)
    {
        cancellation ??= CancellationToken.None;

        var counter = 0;
        foreach(var item in rows)
        {
            if (cancellation.Value.IsCancellationRequested) break;

            try
            {
                var values = new string[columns.Length];
                for (var i = 0; i < columns.Length; i++)
                {
                    var column = columns[i];
                    var obj = column.Property.GetValue(item);
                    var val = column.Formatting(obj);
                    if (val != null && column.Escape) val = Regex.Escape(val);
                    values[i] = val ?? string.Empty;
                }

                writer.WriteLine(string.Join(separator, values));
            }
            finally
            {
                counter++;
            }
        }

        return counter;
    }

    public static async Task<int> WriteAsync(IEnumerable<object> rows, StreamWriter writer, ExportColumn[] columns,string separator, CancellationToken cancellation)
    {
        var counter = 0;
        foreach(var item in rows)
        {
            if (cancellation.IsCancellationRequested) break;

            try
            {
                var values = new string[columns.Length];
                for (var i = 0; i < columns.Length; i++)
                {
                    var column = columns[i];
                    var obj = column.Property.GetValue(item);
                    var val = column.Formatting(obj);
                    if (val != null && column.Escape) val = Regex.Escape(val);
                    values[i] = val ?? string.Empty;
                }

                await writer.WriteLineAsync(string.Join(separator, values));
            }
            finally
            {
                counter++;
            }
        }

        return counter;
    }
}
