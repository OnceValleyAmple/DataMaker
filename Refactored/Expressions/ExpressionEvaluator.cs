using System.Globalization;
using System.Text.RegularExpressions;

namespace DataMaker.Refactored.Expressions;

public sealed class ExpressionException(string message) : Exception(message);

/// <summary>
/// 四则运算表达式求值器。整数与小数均可参与运算，结果以 double 返回；
/// 需要整数的调用方使用 <see cref="EvaluateInt"/>，越界或带小数时会报错。
/// </summary>
public sealed partial class ExpressionEvaluator
{
    const double LongLimit = 9.2233720368547758E18;

    readonly string text; readonly IReadOnlyDictionary<string, long> variables; int index;
    ExpressionEvaluator(string text, IReadOnlyDictionary<string, long> variables) { this.text = text; this.variables = variables; }

    /// <summary>求值并返回 double，允许小数结果。</summary>
    public static double Evaluate(string text, IReadOnlyDictionary<string, long> variables)
    {
        var p = new ExpressionEvaluator(text, variables);
        var value = p.Expression();
        p.Skip();
        if (p.index != p.text.Length) throw new ExpressionException($"表达式存在多余内容：{p.text[p.index..]}");
        if (!double.IsFinite(value)) throw new ExpressionException("表达式结果溢出");
        return value;
    }

    /// <summary>求值并要求结果为 long 范围内的整数。</summary>
    public static long EvaluateInt(string text, IReadOnlyDictionary<string, long> variables)
    {
        var value = Evaluate(text, variables);
        if (Math.Abs(value) > LongLimit) throw new ExpressionException($"表达式结果超出整数范围：{text}");
        return (long)value;
    }

    double Expression()
    {
        var value = Term();
        while (true)
        {
            Skip();
            if (Match('+')) value = Check(value + Term());
            else if (Match('-')) value = Check(value - Term());
            else return value;
        }
    }

    double Term()
    {
        var value = Factor();
        while (true)
        {
            Skip();
            if (Match('*')) value = Check(value * Factor());
            else if (Match('/'))
            {
                var x = Factor();
                if (x == 0) throw new ExpressionException("除数不能为零");
                value = Check(value / x);
            }
            else if (Match('%'))
            {
                var x = Factor();
                if (x == 0) throw new ExpressionException("取模不能为零");
                value %= x;
            }
            else return value;
        }
    }

    double Factor()
    {
        Skip();
        if (Match('-')) return -Factor();
        if (Match('('))
        {
            var x = Expression();
            if (!Match(')')) throw new ExpressionException("缺少右括号");
            return x;
        }
        if (index >= text.Length) throw new ExpressionException("表达式不完整");
        if (char.IsDigit(text[index]) || (text[index] == '.' && index + 1 < text.Length && char.IsDigit(text[index + 1])))
        {
            var start = index;
            while (index < text.Length && char.IsDigit(text[index])) index++;
            if (index < text.Length && text[index] == '.')
            {
                index++;
                if (index >= text.Length || !char.IsDigit(text[index])) throw new ExpressionException("小数点后缺少数字");
                while (index < text.Length && char.IsDigit(text[index])) index++;
            }
            if (!double.TryParse(text[start..index], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                throw new ExpressionException($"无法识别的数字：{text[start..index]}");
            return number;
        }
        if (char.IsLetter(text[index]) || text[index] == '_')
        {
            var start = index++;
            while (index < text.Length && (char.IsLetterOrDigit(text[index]) || text[index] == '_')) index++;
            var name = text[start..index];
            if (!variables.TryGetValue(name, out var value)) throw new ExpressionException($"变量未定义：{name}");
            return value;
        }
        throw new ExpressionException($"无法识别字符：{text[index]}");
    }

    static double Check(double value) => double.IsFinite(value) ? value : throw new ExpressionException("表达式结果溢出");

    bool Match(char c) { Skip(); if (index < text.Length && text[index] == c) { index++; return true; } return false; }
    void Skip() { while (index < text.Length && char.IsWhiteSpace(text[index])) index++; }

    /// <summary>表达式文本中出现的标识符（变量名），按出现顺序去重。</summary>
    public static IReadOnlyList<string> Identifiers(string? text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
        return IdentifierRegex().Matches(text).Select(x => x.Value).Distinct().ToList();
    }

    /// <summary>参数文本是否为空。空文本无法求值，需要在校验阶段拦截。</summary>
    public static bool IsBlank(string? text) => string.IsNullOrWhiteSpace(text);

    [GeneratedRegex(@"[A-Za-z_][A-Za-z_0-9]*")]
    private static partial Regex IdentifierRegex();
}
