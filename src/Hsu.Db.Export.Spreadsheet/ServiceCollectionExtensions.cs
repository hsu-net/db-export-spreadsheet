using Hsu.Db.Export.Spreadsheet.Services;
using Hsu.Db.Export.Spreadsheet.Workers;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable UnusedType.Global

namespace Hsu.Db.Export.Spreadsheet;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDailySyncSpreadsheet(this IServiceCollection services)
    {
        return services
            .AddSingleton<IExportService, ExportService>()
            .AddSingleton<IDbDailySyncWorker, DbDailySyncWorker>()
            .AddHostedService<IDbDailySyncWorker>(x => x.GetRequiredService<IDbDailySyncWorker>());
    }
}