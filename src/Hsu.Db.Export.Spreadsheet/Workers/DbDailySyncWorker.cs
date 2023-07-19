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
// ReSharper disable PossibleMultipleEnumeration

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
            if (await Check(now, _time, stoppingToken)) continue;

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
                            var total = new AsyncLocal<int>();
                            var records = GetRecords(_freeSql, table, date, types[table.Code], total, stoppingToken);
                            if (!records.Any()) continue;
                            await _exportService.ExportAsync(records, table, _path, date.Date, configurations[table.Code], stoppingToken);
                            _logger.LogInformation("Executing synchronously [{Table}]-{Date:yyyy-MM-dd} export {Total} records completed"
                                , table.Name
                                , date
                                , total.Value);
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
                        await Task.Delay(_options.Interval, stoppingToken);
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

    private static IEnumerable<object> GetRecords(IFreeSql freeSql, TableOptions table, DateTime date, TableInfo info, AsyncLocal<int> total, CancellationToken stoppingToken)
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

        var collection = new BlockingCollection<object>();
        Task.Run(() =>
        {
            var count = 0;
            var down = new CountdownEvent(1);
            freeSql
                .Select<object>()
                .AsType(info.Type)
                .WhereDynamicFilter(filter)
                .OrderByPropertyName(table.Filter, table.AscOrder)
                .ToChunk(table.Chunk, x =>
                {
                    try
                    {
                        down.AddCount();
                        Interlocked.Add(ref count, x.Object.Count);
                        foreach(var item in x.Object)
                        {
                            collection.TryAdd(item, Timeout.Infinite, stoppingToken);
                        }
                    }
                    finally
                    {
                        down.Signal();
                    }
                });
            down.Signal();
            down.Wait(stoppingToken);
            total.Value = count;
            collection.CompleteAdding();
        }, stoppingToken);

        while (!collection.IsCompleted)
        {
            if (!collection.TryTake(out var item)) continue;
            yield return item;
        }
    }

    private static (DateTime Date, string Path) GetDate(string dir, string table, DateTime now)
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

        return (date, path);
    }

    private static async Task<bool> Check(DateTime now, TimeSpan span, CancellationToken stoppingToken)
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

            if (!string.IsNullOrWhiteSpace(table.Template))
            {
                if (!table.Template.EndsWith(".xlsx"))
                {
                    _logger.LogWarning("The template file [{File}] extension is not `.xlsx`", table.Template);
                    table.Template = null;
                }
                else
                {
                    var path = Path.Combine(Environment.CurrentDirectory, table.Template);
                    if (!File.Exists(path))
                    {
                        _logger.LogWarning("Could not find template file [{File}]", table.Template);
                        table.Template = null;
                    }    
                }
            }
            else
            {
                table.Template = null;
            }

            if (table.Output.Equals("Xlsx", StringComparison.OrdinalIgnoreCase))
            {
                configurations.TryAdd(table.Code, new OpenXmlConfiguration
                {
                    AutoFilter = false,
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
