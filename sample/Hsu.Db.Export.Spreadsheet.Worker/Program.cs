using FreeSql;
using Hsu.Daemon.Hosting;
using Hsu.Db.Export.Spreadsheet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
// ReSharper disable StringLiteralTypo

if (Hsu.Daemon.Host.Runnable(args, out var code)) return;

var variables = Environment.GetCommandLineArgs();
Environment.SetEnvironmentVariable("AppRoot", variables[0], EnvironmentVariableTarget.Process);

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("serilog.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var builder = Host
    .CreateDefaultBuilder(null)
    .ConfigureHostConfiguration(x => x.SetBasePath(Path.GetDirectoryName(variables[0])!))
    .ConfigureServices((hostContext, services) =>
    {
        ConfigureServices(services, hostContext.Configuration);
    })
    .UseWindowsService()
    .UseSerilog();

var host = builder.Build();
host.Run(code);

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddDailySyncSpreadsheet();
    ConfigureFreeSql(services, configuration);
}

static void ConfigureFreeSql(IServiceCollection services, IConfiguration configuration)
{
    var freeSql = new FreeSqlBuilder()
        .UseConnectionString(DataType.MySql, configuration.GetConnectionString("Default"))
        .Build();

    freeSql.Aop.CommandAfter += (_, e) =>
    {
        if (e.Exception != null)
        {
            Log.Logger.Error("Message:{Message}\r\nStackTrace:{StackTrace}", e.Exception.Message, e.Exception.StackTrace);
        }

        Log.Logger.Debug("FreeSql>{Sql}", e.Command.CommandText);
    };

    services.AddSingleton(freeSql);
}

