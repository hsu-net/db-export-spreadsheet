namespace Hsu.Db.Export.Spreadsheet.Annotations;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ExportDisplayAttribute : Attribute
{
    public string Display { get; set; } = string.Empty;

    public ExportDisplayAttribute()
    {    
    }
    
    public ExportDisplayAttribute(string display)
    {
        Display = display;
    }
}
