using System.Reflection;
using Hsu.Db.Export.Spreadsheet.Annotations;
using Hsu.Db.Export.Spreadsheet.Utils;

namespace Hsu.Db.Export.Spreadsheet;

public record ExportColumn(PropertyInfo Property,bool Escape=false)
{
    private Func<object?, string?> Formatter { get; init; } = new Lazy<Func<object?, string?>>(() => Formatting(Property.PropertyType)).Value;

    public string? Formatting(object? value)
    {
        return Formatter(value);
    }

    private static Func<object?, string?> Formatting(Type type)
    {
        if (type.IsNullable(out var t) && t != null) type = t;
        
        if (type.IsEnum)
        {
            return o => o?.ToString();
        }
        
        var format = type.GetCustomAttribute<FormatAttribute>()?.Format;
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => o =>
            {
                if (!format.IsNullOrWhiteSpace())
                {
                    var array = format!.Split(';');
                    switch (array?.Length)
                    {
                        case 1:
                            return FromBoolean(o, null, null, array[0]);
                        case 2:
                            return FromBoolean(o, array[0], array[1]);
                        case 3:
                            return FromBoolean(o, array[0], array[1], array[2]);
                    }
                }

                return FromBoolean(o);
            },
            TypeCode.DateTime => o =>
            {
                if (format.IsNullOrWhiteSpace())
                {
                    format = "yyyy-MM-dd HH:mm:ss";
                }

                return FromDateTime(o, format!);
            },
            TypeCode.Byte => o => FromFormatter<byte>(o, format ?? string.Empty),
            TypeCode.Int16 => o => FromFormatter<short>(o, format ?? string.Empty),
            TypeCode.Int32 => o => FromFormatter<int>(o, format ?? string.Empty),
            TypeCode.Int64 => o => FromFormatter<long>(o, format ?? string.Empty),
            TypeCode.SByte => o => FromFormatter<sbyte>(o, format ?? string.Empty),
            TypeCode.UInt16 => o => FromFormatter<ushort>(o, format ?? string.Empty),
            TypeCode.UInt32 => o => FromFormatter<uint>(o, format ?? string.Empty),
            TypeCode.UInt64 => o => FromFormatter<ulong>(o, format ?? string.Empty),
            TypeCode.Single => o => FromFormatter<float>(o, format ?? string.Empty),
            TypeCode.Double => o => FromFormatter<double>(o, format ?? string.Empty),
            TypeCode.Decimal => o => FromFormatter<decimal>(o, format ?? string.Empty),
            _ => o => o?.ToString()
        };
    }

    private static string? FromBoolean(object? value, string? trueValue = null, string? falseValue = null, string? nullValue = null)
    {
        if (value == null) return nullValue ?? null;

        if (value is bool v)
        {
            return v ? trueValue ?? bool.TrueString : falseValue ?? bool.FalseString;
        }

        return value.ToString();
    }

    private static string? FromDateTime(object? value, string format)
    {
        if (value == null) return null;
        if (value is DateTime v) return v.ToString(format);
        if (value is DateTimeOffset o) return o.ToString(format);
        return value.ToString();
    }

    private static string? FromFormatter<T>(object? value, string? format) where T : IFormattable
    {
        if (value == null) return null;
        if (value is T v) return v.ToString(format, null);
        return value.ToString();
    }
}
