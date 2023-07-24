using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Hsu.Db.Export.Spreadsheet.OpenXml;

public static class OpenXmlHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CellValues GetCellValueType(Type t)
    {
        return Type.GetTypeCode(t) switch
        {
            TypeCode.Boolean => CellValues.Boolean,
            TypeCode.DateTime => CellValues.Date,
            TypeCode.Byte => CellValues.Number,
            TypeCode.Int16 => CellValues.Number,
            TypeCode.Int32 => CellValues.Number,
            TypeCode.Int64 => CellValues.Number,
            TypeCode.SByte => CellValues.Number,
            TypeCode.UInt16 => CellValues.Number,
            TypeCode.UInt32 => CellValues.Number,
            TypeCode.UInt64 => CellValues.Number,
            TypeCode.Single => CellValues.Number,
            TypeCode.Double => CellValues.Number,
            TypeCode.Decimal => CellValues.Number,
            TypeCode.Object => CellValues.String,
            TypeCode.DBNull => CellValues.String,
            TypeCode.Char => CellValues.String,
            TypeCode.String => CellValues.String,
            _ => CellValues.String
        };
    }
    
    public static string GetCellValue(Cell cell,SharedStringTable? stringTable=null)
    {
        var str = cell.InnerText;
        if (cell.DataType?.Value == CellValues.SharedString && stringTable != null)
        {
            return stringTable.ChildElements[int.Parse(str)].InnerText;
        }

        return str;
    }
}