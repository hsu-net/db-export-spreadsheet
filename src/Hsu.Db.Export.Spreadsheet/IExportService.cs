using FreeSql.Internal.Model;
using Hsu.Db.Export.Spreadsheet.Options;

namespace Hsu.Db.Export.Spreadsheet;

public interface IExportService
{
    Task<int> ExportAsync(IEnumerable<object>? rows, TableInfo info, TableOptions options, string dir, DateTime date, CancellationToken cancellation);
}