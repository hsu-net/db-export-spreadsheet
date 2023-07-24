using System.Collections.Concurrent;
using System.Reflection;
using Hsu.Db.Export.Spreadsheet.Annotations;

namespace Hsu.Db.Export.Spreadsheet.Options;

public class ExportContributions
{
    private readonly ConcurrentDictionary<string, Type> _types;

    public IReadOnlyDictionary<string,Type> Types => _types;
    
    public ExportContributions()
    {
        var type = typeof(IExportService);
        _types = new ConcurrentDictionary<string, Type>();
        var types = type
            .Assembly
            .GetTypes()
            .Where(x => type.IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false, IsGenericType: false })
            .ToArray();
        TryAdd(types);
    }

    public bool TryGet(string output, out Type type)
    {
        return _types.TryGetValue(output.ToLower(), out type);
    }

    public ExportContributions TryAdd<T>() where T : IExportService
    {
        return TryAdd(typeof(T));
    }

    private ExportContributions TryAdd(params Type[] types)
    {
        foreach(var type in types)
        {
            var attribute = type.GetCustomAttribute<ExportAttribute>();
            if (attribute != null)
            {
                _types.TryAdd(attribute.Output.ToLower(), type);
            }
        }

        return this;
    }
}
