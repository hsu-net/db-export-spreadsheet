using System.Runtime.CompilerServices;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace Hsu.Db.Export.Spreadsheet.Utils;

public static class TypeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFromTypeCode(TypeCode typeCode, out Type? type)
    {
        type = null;
        type = typeCode switch
        {
            TypeCode.Object => typeof(object),
            TypeCode.DBNull => typeof(DBNull),
            TypeCode.Boolean => typeof(bool),
            TypeCode.Byte => typeof(byte),
            TypeCode.Char => typeof(char),
            TypeCode.Int16 => typeof(short),
            TypeCode.Int32 => typeof(int),
            TypeCode.Int64 => typeof(long),
            TypeCode.SByte => typeof(sbyte),
            TypeCode.UInt16 => typeof(ushort),
            TypeCode.UInt32 => typeof(uint),
            TypeCode.UInt64 => typeof(ulong),
            TypeCode.Single => typeof(float),
            TypeCode.Double => typeof(double),
            TypeCode.Decimal => typeof(decimal),
            TypeCode.String => typeof(string),
            TypeCode.DateTime => typeof(DateTime),
            _ => null
        };

        return type != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFromTypeCode(string str, out Type? type)
    {
        type = null;
        return Enum.TryParse<TypeCode>(str, true, out var typeCode) && TryFromTypeCode(typeCode, out type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type? TryFromTypeCode(TypeCode typeCode)
    {
        return TryFromTypeCode(typeCode, out var type) ? type : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type? TryFromTypeCode(string str)
    {
        return TryFromTypeCode(str, out var type) ? type : null;
    }
}
