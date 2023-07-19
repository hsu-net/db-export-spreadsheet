// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Hsu.Db.Export.Spreadsheet.Options;

public class ExportOptions
{
    public static string Export { get; set; } = "Export:Spreadsheet"; 
    public TimeSpan Trigger { get; set; } = TimeSpan.Zero;
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
    public bool AscOrder { get; set; } = true;
    public List<TableField> Fields { get; set; }
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
        public string Type { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
    }
}