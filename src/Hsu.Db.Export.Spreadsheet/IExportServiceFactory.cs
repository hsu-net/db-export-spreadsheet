using Hsu.Db.Export.Spreadsheet.Options;
using Hsu.Db.Export.Spreadsheet.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hsu.Db.Export.Spreadsheet;

public interface IExportServiceFactory
{
    IExportService? Get(string output);
}

public class ExportServiceFactory : IExportServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExportContributions _contributions;

    public ExportServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _contributions = _serviceProvider.GetRequiredService<ExportContributions>();
    }

    public IExportService? Get(string output)
    {
        if (!_contributions.TryGet(output, out var type)) return null;
        return _serviceProvider.GetRequiredService(type) as IExportService;
    }
}