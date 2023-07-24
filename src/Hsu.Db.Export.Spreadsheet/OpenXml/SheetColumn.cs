using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
// ReSharper disable RedundantExplicitPositionalPropertyDeclaration

namespace Hsu.Db.Export.Spreadsheet.OpenXml;

public record SheetColumn(PropertyInfo Property, bool Escape = false) : ExportColumn(Property, Escape)
{
    public CellValues CellType { get; init; } = new Lazy<CellValues>(() => OpenXmlHelper.GetCellValueType(Property.PropertyType)).Value;
    public int ColumnIndex { get; set; } = -1;
    public UInt32Value CellStyleIndex { get; set; } = 0;
}
