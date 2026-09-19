namespace DataMaker.Refactored.Expressions;

public sealed class ExpressionException(string message) : Exception(message);
public sealed class ExpressionEvaluator
{
    readonly string text; readonly IReadOnlyDictionary<string,long> variables; int index;
    ExpressionEvaluator(string text,IReadOnlyDictionary<string,long> variables){this.text=text;this.variables=variables;}
    public static long Evaluate(string text,IReadOnlyDictionary<string,long> variables){var p=new ExpressionEvaluator(text,variables);var value=p.Expression();p.Skip();if(p.index!=p.text.Length)throw new ExpressionException($"表达式存在多余内容：{p.text[p.index..]}");return value;}
    long Expression(){var value=Term();while(true){Skip();if(Match('+'))value=checked(value+Term());else if(Match('-'))value=checked(value-Term());else return value;}}
    long Term(){var value=Factor();while(true){Skip();if(Match('*'))value=checked(value*Factor());else if(Match('/')){var x=Factor();if(x==0)throw new ExpressionException("除数不能为零");value/=x;}else if(Match('%')){var x=Factor();if(x==0)throw new ExpressionException("取模不能为零");value%=x;}else return value;}}
    long Factor(){Skip();if(Match('-'))return checked(-Factor());if(Match('(')){var x=Expression();if(!Match(')'))throw new ExpressionException("缺少右括号");return x;}if(index>=text.Length)throw new ExpressionException("表达式不完整");if(char.IsDigit(text[index])){var start=index;while(index<text.Length&&char.IsDigit(text[index]))index++;return long.Parse(text[start..index]);}if(char.IsLetter(text[index])||text[index]=='_'){var start=index++;while(index<text.Length&&(char.IsLetterOrDigit(text[index])||text[index]=='_'))index++;var name=text[start..index];if(!variables.TryGetValue(name,out var value))throw new ExpressionException($"变量未定义：{name}");return value;}throw new ExpressionException($"无法识别字符：{text[index]}");}
    bool Match(char c){Skip();if(index<text.Length&&text[index]==c){index++;return true;}return false;} void Skip(){while(index<text.Length&&char.IsWhiteSpace(text[index]))index++;}
}
