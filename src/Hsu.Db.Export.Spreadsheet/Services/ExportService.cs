using Hsu.Db.Export.Spreadsheet.Options;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
// ReSharper disable PossibleMultipleEnumeration

namespace Hsu.Db.Export.Spreadsheet.Services;

public interface IExportService
{
    Task ExportAsync(IEnumerable<dynamic>? rows, TableOptions options,string dir, DateTime date,Configuration? configuration, CancellationToken cancellation);
}

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public async Task ExportAsync(IEnumerable<object>? rows, TableOptions options,string dir, DateTime date,Configuration? configuration, CancellationToken cancellation)
    {
        //if(rows==null) return;
        if (options.Template != null)
        {
            await MiniExcel
                .SaveAsByTemplateAsync(Path.Combine(dir,$"{options.Name}-{date:yyyy-MM-dd}.xlsx")
                    ,Path.Combine(Environment.CurrentDirectory,options.Template!)
                    ,new {_=rows},null,cancellation);
        }
        else
        {
            var xlsx = "Xlsx".Equals(options.Output, StringComparison.OrdinalIgnoreCase);
            await MiniExcel.SaveAsAsync(Path.Combine(dir, $"{options.Name}-{date:yyyy-MM-dd}.{(xlsx ? "xlsx" : "csv")}")
                , rows
                , true
                , options.Name
                , xlsx ? ExcelType.XLSX : ExcelType.CSV
                , configuration
                , true
                , cancellation);
        }
    }
}
