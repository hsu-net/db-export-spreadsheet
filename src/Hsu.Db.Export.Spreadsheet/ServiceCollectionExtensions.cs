using Hsu.Db.Export.Spreadsheet.Options;
using Hsu.Db.Export.Spreadsheet.Workers;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable UnusedType.Global

namespace Hsu.Db.Export.Spreadsheet;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDailySyncSpreadsheet(this IServiceCollection services, Action<ExportContributions>? configure = null)
    {
        var ec = new ExportContributions();
        configure?.Invoke(ec);

        services = ec.Types.Values.Aggregate(services, (current, type) => current.AddSingleton(type));

        return services
            .AddSingleton(ec)
            .AddSingleton<IExportServiceFactory, ExportServiceFactory>()
            .AddSingleton<IDbDailySyncWorker, DbDailySyncWorker>()
            .AddHostedService<IDbDailySyncWorker>(x => x.GetRequiredService<IDbDailySyncWorker>());
    }
}