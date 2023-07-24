using System.ComponentModel;
using System.Reflection;
using DocumentFormat.OpenXml.Spreadsheet;
using Hsu.Db.Export.Spreadsheet.Annotations;
using Hsu.Db.Export.Spreadsheet.Utils;

namespace Hsu.Db.Export.Spreadsheet.OpenXml;

public static class XlsxGenWriter
{
    public static int Write(IEnumerable<object> rows, SheetData sheetData, SheetColumn[] columns, CancellationToken cancellation)
    {
        var headerRow = new Row();
        foreach(var column in columns)
        {
            var columnName = column.Property.Name;
            if (column.Property.GetCustomAttribute<ExportDisplayAttribute>() is { } exportDisplay && !exportDisplay.Display.IsNullOrWhiteSpace())
            {
                columnName = exportDisplay.Display;
            }

            headerRow.AppendChild(new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(columnName)
            });
        }
        sheetData.AppendChild(headerRow);

        return XlsxWriter.Write(rows, sheetData, columns, cancellation);
    }
}
