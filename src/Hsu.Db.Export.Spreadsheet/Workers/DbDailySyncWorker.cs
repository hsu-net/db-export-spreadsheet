using System.Collections.Concurrent;
using System.Globalization;
using FreeSql.DataAnnotations;
using FreeSql.Internal.Model;
using Hsu.Db.Export.Spreadsheet.Options;
using Hsu.Db.Export.Spreadsheet.Services;
using Hsu.Db.Export.Spreadsheet.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using MiniExcelLibs.Csv;
using MiniExcelLibs.OpenXml;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
// ReSharper disable UnusedMember.Local

namespace Hsu.Db.Export.Spreadsheet.Workers;

public interface IDbDailySyncWorker : IHostedService { }

public class DbDailySyncWorker : BackgroundService, IDbDailySyncWorker
{
    private readonly ILogger<DbDailySyncWorker> _logger;
    private readonly IFreeSql _freeSql;
    private readonly IExportService _exportService;
    private readonly ExportOptions _options;
    private readonly TimeSpan _time;
    private readonly string _path;

    public DbDailySyncWorker(ILogger<DbDailySyncWorker> logger, IFreeSql freeSql, IConfiguration configuration, IExportService exportService)
    {
        _options = configuration.GetSection(ExportOptions.Export).Get<ExportOptions>() ?? throw new InvalidOperationException();
        _path = string.IsNullOrWhiteSpace(_options.Path) ? Path.Combine(Environment.CurrentDirectory, "Data") : _options.Path;
        _time = _options.Trigger;
        _logger = logger;
        _freeSql = freeSql;
        _exportService = exportService;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sync Starting");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sync Stopping");
        base.StopAsync(cancellationToken);
        _freeSql.Dispose();
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sync Executing");
        var dir = InitialDirectory();

        var (types, configurations) = InitialConfigurations();

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            if (await Check(now,_time,stoppingToken)) continue;
            
            try
            {
                now = now.AddDays(-1);
                _logger.LogInformation("Executing synchronously mes data....");
                var completed = false;
                while (!completed)
                {
                    completed = true;
                    foreach(var table in _options.Tables)
                    {
                        var (date, tmp) = GetDate(dir, table.Code, now);
                        if (date > now) continue;
                        if (date < now) completed = false;
                        try
                        {
                            _logger.LogInformation("Executing synchronously [{Table}]-{Date:yyyy-MM-dd} records ...", table.Name, date);
                            var records = await GetRecords(_freeSql,table, date, types[table.Code], stoppingToken);
                            _logger.LogInformation("Executing synchronously [{Table}]-{Date:yyyy-MM-dd} query {Count} records ..."
                                , table.Name
                                , date
                                , records?.Count ?? 0);
                            
                            if (records == null || records.Count == 0) continue;

                            await _exportService.ExportAsync(records, table, _path, date.Date, configurations[table.Code], stoppingToken);
                            _logger.LogInformation("Executing synchronously [{Table}]-{Date:yyyy-MM-dd} export {Count} records completed"
                                , table.Name
                                , date
                                , records.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                            completed = true;
                            break;
                        }
                        finally
                        {
                            #if NETFRAMEWORK || NETSTANDARD2_0
                            File.WriteAllText(tmp, $"{date.AddDays(1):yyyy-MM-dd}");
                            #else
                            await File.WriteAllTextAsync(tmp, $"{date.AddDays(1):yyyy-MM-dd}", stoppingToken);
                            #endif
                        }
                    }

                    if (!completed)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }

                _logger.LogInformation("Executing synchronously mes data completed");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private static async Task<List<object>> GetRecords(IFreeSql freeSql,TableOptions table, DateTime date, TableInfo info, CancellationToken stoppingToken)
    {
        var filter = new DynamicFilterInfo()
        {
            Logic = DynamicFilterLogic.And,
            Filters = new List<DynamicFilterInfo>()
            {
                new()
                {
                    Field = $"a.{table.Filter}",
                    Operator = DynamicFilterOperator.GreaterThanOrEqual,
                    Value = date.Date
                },
                new()
                {
                    Field = $"a.{table.Filter}",
                    Operator = DynamicFilterOperator.LessThan,
                    Value = date.Date.AddDays(1)
                }
            }
        };

        var records = await freeSql
            .Select<object>()
            .AsType(info.Type)
            .WhereDynamicFilter(filter)
            .OrderByPropertyName(table.Filter, table.AscOrder)
            .ToListAsync(stoppingToken);
        return records;
    }

    private static (DateTime Date,string Path) GetDate(string dir, string table, DateTime now)
    {
        DateTime date;
        var path = Path.Combine(dir, table);
        if (File.Exists(path))
        {
            var txt = File.ReadAllText(path);
            date = !string.IsNullOrWhiteSpace(txt)
                ? DateTime.TryParseExact(txt, "yyyy-MM-dd", null, DateTimeStyles.None, out var dt)
                    ? dt
                    : now
                : now;
        }
        else
        {
            date = now;
        }

        return (date,path);
    }

    private static async Task<bool> Check(DateTime now,TimeSpan span,CancellationToken stoppingToken)
    {
        var t1 = now.Date.Add(span - TimeSpan.FromHours(1));
        var t2 = now.Date.Add(span - TimeSpan.FromMinutes(15));
        var t3 = now.Date.Add(span - TimeSpan.FromMinutes(1));
        var t4 = now.Date.Add(span);
        var t5 = now.Date.Add(span + TimeSpan.FromSeconds(59));

        if (now < t1 || now > t5)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            return true;
        }

        if (now < t4)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            return true;
        }

        if (now < t3)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            return true;
        }

        if (now < t2)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            return true;
        }

        return false;
    }

    private (ConcurrentDictionary<string, TableInfo> Types, ConcurrentDictionary<string, Configuration> Configurations) InitialConfigurations()
    {
        var types = new ConcurrentDictionary<string, TableInfo>();
        var configurations = new ConcurrentDictionary<string, Configuration>();
        foreach(var table in _options.Tables)
        {
            var builder = _freeSql.CodeFirst.DynamicEntity(table.Code, new TableAttribute { Name = table.Code });
            foreach(var field in table.Fields)
            {
                if (!TypeHelper.TryFromTypeCode(field.Type, out var type)) type = typeof(string);
                builder.Property(field.Column, type); //new DisplayNameAttribute(field.Name)
            }

            types.TryAdd(table.Code, builder.Build());

            if (table.Output.Equals("Xlsx", StringComparison.OrdinalIgnoreCase))
            {
                configurations.TryAdd(table.Code, new OpenXmlConfiguration
                {
                    DynamicColumns = table.Fields.Select(x => new DynamicExcelColumn(x.Column) { Name = x.Name }).ToArray()
                });
            }
            else
            {
                configurations.TryAdd(table.Code, new CsvConfiguration()
                {
                    DynamicColumns = table.Fields.Select(x => new DynamicExcelColumn(x.Column) { Name = x.Name }).ToArray()
                });
            }
        }

        return (types, configurations);
    }

    private string InitialDirectory()
    {
        var dir = Path.Combine(Environment.CurrentDirectory, ".tmp");
        if (!Directory.Exists(dir))
        {
            try
            {
                var di = Directory.CreateDirectory(dir);
                di.Attributes |= FileAttributes.Hidden;
                _logger.LogInformation("Create Directory {Dir}", dir);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        try
        {
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
                _logger.LogInformation("Create Directory {Dir}", _path);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }

        return dir;
    }
}
