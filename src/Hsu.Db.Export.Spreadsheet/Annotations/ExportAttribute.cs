namespace Hsu.Db.Export.Spreadsheet.Annotations;

[AttributeUsage(AttributeTargets.Class)]
public class ExportAttribute : Attribute
{
    public string Output { get; set; }

    public ExportAttribute(string output)
    {
        Output = output;
    }
}
