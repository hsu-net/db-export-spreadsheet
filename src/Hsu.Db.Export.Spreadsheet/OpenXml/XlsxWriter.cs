using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Hsu.Db.Export.Spreadsheet.OpenXml;

public static class XlsxWriter
{
    public static int Write(IEnumerable<object> rows, SheetData sheetData, SheetColumn[] columns, CancellationToken cancellation)
    {
        var counter = 0;
        foreach(var item in rows)
        {
            if (cancellation.IsCancellationRequested) break;

            try
            {
                var dataRow = new Row();
                foreach(var column in columns)
                {
                    var obj = column.Property.GetValue(item);
                    var val = column.Formatting(obj);
                    if (val != null && column.Escape) val = Regex.Escape(val);

                    var cell = new Cell
                    {
                        StyleIndex = column.CellStyleIndex > 0 ? new UInt32Value(column.CellStyleIndex) : null,
                        DataType = column.CellType,
                        CellValue = val ==  null ? new CellValue() : new CellValue(val)
                    };
                    dataRow.AppendChild(cell);
                }

                sheetData.AppendChild(dataRow);
            }
            finally
            {
                counter++;
            }
        }

        return counter;
    }
}
