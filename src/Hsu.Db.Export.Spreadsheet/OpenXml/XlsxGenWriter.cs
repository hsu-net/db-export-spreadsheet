using System.ComponentModel;
using System.Reflection;
using DocumentFormat.OpenXml.Spreadsheet;
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
            if (column.Property.GetCustomAttribute<DescriptionAttribute>() is { } description && !description.Description.IsNullOrWhiteSpace())
            {
                columnName = description.Description;
            }
            if (column.Property.GetCustomAttribute<DisplayNameAttribute>() is { } display && !display.DisplayName.IsNullOrWhiteSpace())
            {
                columnName = display.DisplayName;
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
