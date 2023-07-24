// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace Hsu.Db.Export.Spreadsheet.Annotations;

[AttributeUsage(AttributeTargets.Property)]
public class FormatAttribute : Attribute
{
    public string? Format { get; set; }
}
