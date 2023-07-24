using System.ComponentModel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FreeSql.Internal.Model;
using Hsu.Db.Export.Spreadsheet.Annotations;
using Hsu.Db.Export.Spreadsheet.OpenXml;
using Hsu.Db.Export.Spreadsheet.Options;
using Hsu.Db.Export.Spreadsheet.Utils;

namespace Hsu.Db.Export.Spreadsheet.Services;

[Export("Xlsx")]
public class XlsxExportService : IExportService
{
    public Task<int> ExportAsync(IEnumerable<object>? rows, TableInfo info, TableOptions options, string dir, DateTime date, CancellationToken cancellation)
    {
        return Task.Run(() => Export(rows, info, options, dir, date, cancellation), cancellation);
    }

    private int Export(IEnumerable<object>? rows, TableInfo info, TableOptions options, string dir, DateTime date, CancellationToken cancellation)
    {
        if (rows == null) return -1;
        var file = Path.Combine(dir, $"{options.Name}-{date:yyyy-MM-dd}.xlsx");
        using var document = CreateDocument(file, options.Name, options.Template);
        var counter = options.Template == null
            ? XlsxGenWriter.Write(rows, document.WorkbookPart!.WorksheetParts.First().Worksheet.GetFirstChild<SheetData>()!, GetColumns(info), cancellation)
            : XlsxTemplateWriter.Write(rows, document.WorkbookPart!, GetColumns(info), cancellation);
        document.Save();
        return counter;
    }

    private SpreadsheetDocument CreateDocument(string file, string sheetName, string? template)
    {
        var document = SpreadsheetDocument.Create(file, SpreadsheetDocumentType.Workbook, true);
        if (!template.IsNullOrWhiteSpace() && File.Exists(template))
        {
            using var temp = SpreadsheetDocument.Open(template!, false);
            document.AddPart(temp.WorkbookPart!);
            document.WorkbookPart!.WorkbookStylesPart!.Stylesheet.InnerXml = temp.WorkbookPart!.WorkbookStylesPart!.Stylesheet.InnerXml;

            var shared = temp.WorkbookPart?.SharedStringTablePart?.SharedStringTable;
            if (shared == null) return document;

            // If the new file doesn't have a shared string table, create one
            if (document.WorkbookPart.SharedStringTablePart == null)
            {
                document.WorkbookPart.AddNewPart<SharedStringTablePart>();
            }

            document.WorkbookPart.SharedStringTablePart!.SharedStringTable = (SharedStringTable)shared.CloneNode(true);
            // Now, copy the shared string table from the template to the new file
            document.WorkbookPart.SharedStringTablePart.SharedStringTable.Save();

            return document;
        }

        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new SheetData());

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        var sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = sheetName };
        sheets.Append(sheet);

        return document;
    }
    
    private static SheetColumn[] GetColumns(TableInfo info)
    {
        return info
            .Properties
            .Select(x => new SheetColumn(x.Value))
            .ToArray();
    }
}
