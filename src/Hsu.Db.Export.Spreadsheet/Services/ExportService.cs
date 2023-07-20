using System.Collections.Concurrent;
using Hsu.Db.Export.Spreadsheet.Options;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.Csv;
using MiniExcelLibs.OpenXml;

namespace Hsu.Db.Export.Spreadsheet.Services;

public interface IExportService
{
    Task ExportAsync(IEnumerable<object>? rows, TableOptions options, string dir, DateTime date, CancellationToken cancellation);

    void InitialConfigurations(List<TableOptions> tables);
}

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly ConcurrentDictionary<string, Configuration> _configurations;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
        _configurations = new ConcurrentDictionary<string, Configuration>();
    }

    public async Task ExportAsync(IEnumerable<object>? rows, TableOptions options, string dir, DateTime date, CancellationToken cancellation)
    {
        if (rows == null) return;

        try
        {
            var configuration = _configurations[options.Code];
            if (options.Template != null)
            {
                await MiniExcel
                    .SaveAsByTemplateAsync(Path.Combine(dir, $"{options.Name}-{date:yyyy-MM-dd}.xlsx")
                        , Path.Combine(Environment.CurrentDirectory, options.Template!)
                        , new { _ = rows.ToList() }, configuration, cancellation);
            }
            else
            {
                var xlsx = "Xlsx".Equals(options.Output, StringComparison.OrdinalIgnoreCase);
                await MiniExcel.SaveAsAsync(Path.Combine(dir, $"{options.Name}-{date:yyyy-MM-dd}.{(xlsx ? "xlsx" : "csv")}")
                    , xlsx? rows.ToList() : rows
                    , true
                    , options.Name
                    , xlsx ? ExcelType.XLSX : ExcelType.CSV
                    , configuration
                    , true
                    , cancellation);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw;
        }
    }

    public void InitialConfigurations(List<TableOptions> tables)
    {
        foreach(var table in tables)
        {
            if (table.Output.Equals("Xlsx", StringComparison.OrdinalIgnoreCase))
            {
                _configurations.TryAdd(table.Code, new OpenXmlConfiguration
                {
                    AutoFilter = false,
                    FastMode = true,
                    DynamicColumns = table.Fields.Select(x => new DynamicExcelColumn(x.Property ?? x.Column) { Name = x.Name }).ToArray()
                });
            }
            else
            {
                _configurations.TryAdd(table.Code, new CsvConfiguration()
                {
                    FastMode = true,
                    DynamicColumns = table.Fields.Select(x => new DynamicExcelColumn(x.Property ?? x.Column) { Name = x.Name }).ToArray()
                });
            }
        }
    }
}
