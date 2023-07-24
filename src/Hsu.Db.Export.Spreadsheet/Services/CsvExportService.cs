using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using FreeSql.Internal.Model;
using Hsu.Db.Export.Spreadsheet.Annotations;
using Hsu.Db.Export.Spreadsheet.Csv;
using Hsu.Db.Export.Spreadsheet.Options;
using Hsu.Db.Export.Spreadsheet.Utils;

namespace Hsu.Db.Export.Spreadsheet.Services;

[Export("csv")]
public class CsvExportService : IExportService
{
    private static readonly string Separator = ",";
    
    public int Export(IEnumerable<object>? rows, TableInfo info, TableOptions options, string dir, DateTime date, CancellationToken cancellation)
    {
        if (rows == null) return -1;
        var file = Path.Combine(dir, $"{options.Name}-{date:yyyy-MM-dd}.csv");

        using var stream = File.Create(file);
        using var writer = new StreamWriter(stream);

        var columns = GetColumns(info);
        var values = new string[columns.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            var column = columns[i];
            var columnName = column.Property.Name;
            if (column.Property.GetCustomAttribute<DescriptionAttribute>() is { } description && !description.Description.IsNullOrWhiteSpace())
            {
                columnName = description.Description;
            }

            if (column.Property.GetCustomAttribute<DisplayNameAttribute>() is { } display && !display.DisplayName.IsNullOrWhiteSpace())
            {
                columnName = display.DisplayName;
            }

            values[i] = columnName;
        }

        writer.WriteLine(string.Join(Separator, values));
        
        var counter = CsvWriter.Write(rows, writer,columns, Separator, cancellation);
        stream.Flush();
        return counter;
    }

    public async Task<int> ExportAsync(IEnumerable<object>? rows, TableInfo info, TableOptions options, string dir, DateTime date, CancellationToken cancellation)
    {
        if (rows == null) return -1;
        var file = Path.Combine(dir, $"{options.Name}-{date:yyyy-MM-dd}.csv");

        #if NETFRAMEWORK
        using var stream = File.Create(file);
        using var writer = new StreamWriter(stream);
        #else
        await using var stream = File.Create(file);
        await using var writer = new StreamWriter(stream);
        #endif

        var columns = GetColumns(info);
        var values = new string[columns.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            var column = columns[i];
            var columnName = column.Property.Name;
            if (column.Property.GetCustomAttribute<ExportDisplayAttribute>() is { } exportDisplay && !exportDisplay.Display.IsNullOrWhiteSpace())
            {
                columnName = exportDisplay.Display;
            }

            values[i] = columnName;
        }

        await writer.WriteLineAsync(string.Join(Separator, values));
        
        var counter = await CsvWriter.WriteAsync(rows, writer,columns, Separator, cancellation);
        await stream.FlushAsync(cancellation);
        return counter;
    }
    
    private static ExportColumn[] GetColumns(TableInfo info)
    {
        return info
            .Properties
            .Select(x => new ExportColumn(x.Value))
            .ToArray();
    }
}
