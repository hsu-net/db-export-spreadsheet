// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
using System.ComponentModel;

namespace Hsu.Db.Export.Spreadsheet.Options;

public class ExportOptions
{
    public static string Export { get; set; } = "Export:Spreadsheet";
    public TimeSpan Trigger { get; set; } = TimeSpan.Zero;
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    [DefaultValue("00:01:30")]
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(60);
    [DefaultValue(false)]
    public bool? Launch { get; set; }
    public string Path { get; set; } = string.Empty;
    public  List<TableOptions> Tables { get; set; }

    public ExportOptions()
    {
        Tables = new List<TableOptions>();
    }
}

public record TableOptions
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
    [DefaultValue(1000)]
    public int? Chunk { get; set; } = 1000;
    [DefaultValue(true)]
    public bool? AscOrder { get; set; } = true;
    public List<TableField> Fields { get; set; }
    [DefaultValue("Csv")]
    public string Output { get; set; } = "Csv";
    public string? Template { get; set; }

    public TableOptions()
    {
        Fields = new List<TableField>();
    }

    public record TableField
    {
        public string Name { get; set; } = string.Empty;
        public string Column { get; set; } = string.Empty;
        [DefaultValue(null)]
        public string? Property { get; set; }
        [DefaultValue("String")]
        public string Type { get; set; } = "String";
        [DefaultValue(true)]
        public bool? Nullable { get; set; } = true;
        [DefaultValue(null)]
        public string? Format { get; set; }
    }
}