using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using static Hsu.Db.Export.Spreadsheet.OpenXml.OpenXmlHelper;

namespace Hsu.Db.Export.Spreadsheet.OpenXml;

public static class XlsxTemplateWriter
{
    public static int Write(IEnumerable<object> rows, WorkbookPart workbookPart, SheetColumn[] columns, CancellationToken cancellation)
    {
        var stringTable = workbookPart!.SharedStringTablePart?.SharedStringTable;
        var worksheet = workbookPart.WorksheetParts.First().Worksheet;
        var records = worksheet.Descendants<Row>();
        var sheetColumns = new List<SheetColumn>();
        var index = 0;
        foreach(var row in records)
        {
            index++;
            if (index > 2)
            {
                row.Remove();
                continue;
            }
            if(index==1) continue;

            var cells = row.Descendants<Cell>();
            var columnIndex = -1;
            foreach(var cell in cells)
            {
                columnIndex++;
                var raw = GetCellValue(cell, stringTable).Trim();
                var column = columns.FirstOrDefault(x => x.Property.Name.Equals(raw, StringComparison.OrdinalIgnoreCase));
                if (column == null) continue;
                column.ColumnIndex = columnIndex;
                if (cell.StyleIndex?.HasValue == true) column.CellStyleIndex = cell.StyleIndex;
                sheetColumns.Add(column);
            }
            row.Remove();
        }

        var sheetData = worksheet.GetFirstChild<SheetData>()!;
        //sheetData.LastChild!.Remove();
        return XlsxWriter.Write(rows, sheetData, sheetColumns.ToArray(), cancellation);
    }
}
