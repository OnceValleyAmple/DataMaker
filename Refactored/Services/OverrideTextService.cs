using DataMaker.Refactored.Models;

namespace DataMaker.Refactored.Services;

/// <summary>
/// Subtask 覆盖值的双向文本表示。
/// 覆盖矩阵单元格、覆盖编辑弹窗、旧版 JSON 迁移共用同一套 key，避免出现「弹窗能解析、单元格不能解析」这类不一致。
/// </summary>
public static class OverrideTextService
{
    /// <summary>把 <c>key=value;key=value</c> 文本解析成强类型覆盖对象；空文本返回 null 表示「继承全局」。</summary>
    public static FieldOverrideBase? Parse(FieldConfig field, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var v = text.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .GroupBy(x => x[0].Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last()[1].Trim(), StringComparer.OrdinalIgnoreCase);

        return field switch
        {
            IntField => new IntegerFieldOverride { Min = Get(v, "min"), Max = Get(v, "max") },
            FloatField => new FloatFieldOverride { Min = Get(v, "min"), Max = Get(v, "max"), Precision = GetInt(v, "precision") },
            StringField => new StringFieldOverride
            {
                Length = Get(v, "length"),
                MinLength = Get(v, "minLength"),
                MaxLength = Get(v, "maxLength"),
                CustomCharset = Get(v, "charset"),
                CharsetPreset = GetEnum<CharsetPreset>(v, "charsetPreset"),
                Pattern = GetEnum<StringPattern>(v, "pattern")
            },
            ArrayField => new ArrayFieldOverride
            {
                Length = Get(v, "length"),
                Min = Get(v, "min"),
                Max = Get(v, "max"),
                Precision = GetInt(v, "precision"),
                Pattern = GetEnum<ArrayPattern>(v, "pattern"),
                SortOrder = GetEnum<ArraySortOrder>(v, "sort"),
                Unique = GetBool(v, "unique"),
                Charset = Get(v, "charset"),
                CharsetPreset = GetEnum<CharsetPreset>(v, "charsetPreset"),
                Separator = GetEnum<ArraySeparator>(v, "separator"),
                StringMinLength = Get(v, "stringMinLength"),
                StringMaxLength = Get(v, "stringMaxLength")
            },
            TreeField => new TreeFieldOverride
            {
                Nodes = Get(v, "nodes"),
                Root = GetInt(v, "root"),
                Shape = GetEnum<TreeShape>(v, "shape"),
                WeightMin = Get(v, "weightMin"),
                WeightMax = Get(v, "weightMax")
            },
            GraphField => new GraphFieldOverride
            {
                Density = GetDouble(v, "density"),
                VertexCount = Get(v, "nodes"),
                EdgeCount = Get(v, "edges"),
                SpecialType = GetEnum<GraphSpecialType>(v, "shape"),
                WeightMin = Get(v, "weightMin"),
                WeightMax = Get(v, "weightMax")
            },
            _ => null
        };
    }

    /// <summary>把强类型覆盖对象渲染成覆盖矩阵单元格文本；未覆盖任何项时返回「(空覆盖)」。</summary>
    public static string ToText(FieldOverrideBase value) => value switch
    {
        IntegerFieldOverride x => Join(("min", x.Min), ("max", x.Max)),
        FloatFieldOverride x => Join(("min", x.Min), ("max", x.Max), ("precision", x.Precision?.ToString())),
        StringFieldOverride x => Join(("length", x.Length), ("minLength", x.MinLength), ("maxLength", x.MaxLength),
            ("charset", x.CustomCharset), ("charsetPreset", x.CharsetPreset?.ToString()), ("pattern", x.Pattern?.ToString())),
        ArrayFieldOverride x => Join(("length", x.Length), ("min", x.Min), ("max", x.Max), ("precision", x.Precision?.ToString()),
            ("pattern", x.Pattern?.ToString()), ("sort", x.SortOrder?.ToString()), ("unique", x.Unique?.ToString()),
            ("charset", x.Charset), ("separator", x.Separator?.ToString()),
            ("stringMinLength", x.StringMinLength), ("stringMaxLength", x.StringMaxLength)),
        TreeFieldOverride x => Join(("nodes", x.Nodes), ("root", x.Root?.ToString()), ("shape", x.Shape?.ToString()),
            ("weightMin", x.WeightMin), ("weightMax", x.WeightMax)),
        GraphFieldOverride x => Join(("density", x.Density?.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            ("nodes", x.VertexCount), ("edges", x.EdgeCount), ("shape", x.SpecialType?.ToString()),
            ("weightMin", x.WeightMin), ("weightMax", x.WeightMax)),
        _ => "(空覆盖)"
    };

    static string Join(params (string Key, string? Value)[] pairs)
    {
        var parts = pairs.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"{x.Key}={x.Value}").ToList();
        return parts.Count == 0 ? "(空覆盖)" : string.Join(";", parts);
    }

    static string? Get(Dictionary<string, string> v, string key)
        => v.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    static int? GetInt(Dictionary<string, string> v, string key)
        => int.TryParse(Get(v, key), out var x) ? x : null;

    static bool? GetBool(Dictionary<string, string> v, string key)
        => bool.TryParse(Get(v, key), out var x) ? x : null;

    static double? GetDouble(Dictionary<string, string> v, string key)
        => double.TryParse(Get(v, key), System.Globalization.NumberStyles.Float,
               System.Globalization.CultureInfo.InvariantCulture, out var x) ? x : null;

    static T? GetEnum<T>(Dictionary<string, string> v, string key) where T : struct
        => Enum.TryParse<T>(Get(v, key), true, out var x) ? x : null;
}
