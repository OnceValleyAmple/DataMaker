using DataMaker.Refactored.Models;
namespace DataMaker.Refactored.Services;
public sealed record ValidationIssue(string Path,string Message);
public static class ValidationService
{
 public static IReadOnlyList<ValidationIssue> Validate(ProjectConfig c){var result=new List<ValidationIssue>();if(c.TestCount<1)result.Add(new("testCount","测试点数量必须大于 0"));if(c.TimeLimitMs<1)result.Add(new("timeLimitMs","时间限制必须大于 0"));if(c.MemoryLimitMb<0)result.Add(new("memoryLimitMb","内存限制不能为负数"));var names=new HashSet<string>();foreach(var f in c.Fields){if(string.IsNullOrWhiteSpace(f.Name))result.Add(new("fields","字段名称不能为空"));else if(!names.Add(f.Name))result.Add(new($"fields.{f.Name}","字段名称重复"));if(f is StringField sf&&int.TryParse(sf.MinLength,out var smin)&&int.TryParse(sf.MaxLength,out var smax)&&smax<smin)result.Add(new($"fields.{f.Name}","字符串长度范围无效"));if(f is FloatField ff){foreach(var issue in ExpressionPairIssues(f.Name,ff.Min,ff.Max))result.Add(issue);}if(f is TreeField t){if(t.Nodes=="0")result.Add(new($"fields.{f.Name}","树节点数不能为 0"));if(t.IndexStart<0)result.Add(new($"fields.{f.Name}","起始编号不能为负数"));if(int.TryParse(t.Nodes,out var nodeCount)&&(t.Root<t.IndexStart||t.Root>=t.IndexStart+nodeCount))result.Add(new($"fields.{f.Name}","根节点不在树的编号范围内"));}if(f is GraphField g){if(g.Dag&&g.AllowSelfLoops)result.Add(new($"fields.{f.Name}","DAG 不能允许自环"));if(g.IndexStart<0)result.Add(new($"fields.{f.Name}","起始编号不能为负数"));if(int.TryParse(g.Nodes,out var gn)&&int.TryParse(g.Edges,out var ge)&&ge<0)result.Add(new($"fields.{f.Name}","边数不能为负数"));if(g.LeftPartSize<0)result.Add(new($"fields.{f.Name}","二分图左部节点数不能为负数"));if(g.Density<0||g.Density>1)result.Add(new($"fields.{f.Name}","图密度必须在 0 到 1 之间"));if(int.TryParse(g.Nodes,out var gnodes)&&int.TryParse(g.Edges,out var gedges)){var maxEdges=g.AllowSelfLoops?(g.Directed?gnodes*gnodes:gnodes*(gnodes+1)/2):(g.Directed?gnodes*Math.Max(0,gnodes-1):gnodes*Math.Max(0,gnodes-1)/2);if(gedges>maxEdges&&!g.AllowMultiEdges)result.Add(new($"fields.{f.Name}",$"边数超过当前约束允许的最大值 {maxEdges}"));}}}if(c.Groups.Any()){if(c.Groups.Any(g=>g.From<1||g.To<g.From||g.To>c.TestCount))result.Add(new("groups","所有 Subtask 都必须设置有效的测试点编号范围"));if(c.Groups.SelectMany(g=>Enumerable.Range(g.From,Math.Max(0,g.To-g.From+1))).Distinct().Count()!=c.Groups.Sum(g=>Math.Max(0,g.To-g.From+1)))result.Add(new("groups","Subtask 测试点范围存在重叠"));}foreach(var f in c.Fields)foreach(var issue in FieldExpressionIssues(f))result.Add(issue);for(var i=0;i<c.Fields.Count;i++){var laterNames=c.Fields.Skip(i+1).Select(x=>x.Name).ToHashSet();foreach(var reference in ReferencedVariables(c.Fields[i]))if(laterNames.Contains(reference))result.Add(new($"fields.{c.Fields[i].Name}",$"引用了后置字段 {reference}"));}foreach(var group in c.Groups){foreach(var entry in group.TypedOverrides){if(entry.Value is IntegerFieldOverride io&&long.TryParse(io.Min,out var imin)&&long.TryParse(io.Max,out var imax)&&imax<imin)result.Add(new($"groups.{group.Name}.{entry.Key}","整数覆盖范围无效"));if(entry.Value is FloatFieldOverride fo&&double.TryParse(fo.Min,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var fmin)&&double.TryParse(fo.Max,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var fmax)&&fmax<fmin)result.Add(new($"groups.{group.Name}.{entry.Key}","浮点覆盖范围无效"));if(entry.Value is StringFieldOverride so){if(int.TryParse(so.MinLength,out var slmin)&&slmin<0)result.Add(new($"groups.{group.Name}.{entry.Key}","字符串最小长度不能为负数"));if(int.TryParse(so.MaxLength,out var slmax)&&slmax<0)result.Add(new($"groups.{group.Name}.{entry.Key}","字符串最大长度不能为负数"));if(int.TryParse(so.MinLength,out slmin)&&int.TryParse(so.MaxLength,out slmax)&&slmax<slmin)result.Add(new($"groups.{group.Name}.{entry.Key}","字符串长度范围无效"));if(so.CustomCharset is not null&&so.CustomCharset.Length==0)result.Add(new($"groups.{group.Name}.{entry.Key}","字符串字符集不能为空"));}if(entry.Value is ArrayFieldOverride ao){if(int.TryParse(ao.Length,out var al)&&al<0)result.Add(new($"groups.{group.Name}.{entry.Key}","数组长度不能为负数"));if(long.TryParse(ao.Min,out var amin)&&long.TryParse(ao.Max,out var amax)&&amax<amin)result.Add(new($"groups.{group.Name}.{entry.Key}","数组元素范围无效"));if(ao.Pattern is ArrayPattern.StrictIncreasing or ArrayPattern.StrictDecreasing&&int.TryParse(ao.Length,out var strictLength)&&long.TryParse(ao.Min,out amin)&&long.TryParse(ao.Max,out amax)&&amax-amin+1<strictLength)result.Add(new($"groups.{group.Name}.{entry.Key}","数组范围不足以满足严格单调性质"));}if(entry.Value is GraphFieldOverride go&&go.Density is < 0 or > 1)result.Add(new($"groups.{group.Name}.{entry.Key}","图密度必须在 0 到 1 之间"));if(entry.Value is GraphFieldOverride go2&&long.TryParse(go2.VertexCount,out var vn)&&vn<1)result.Add(new($"groups.{group.Name}.{entry.Key}","图节点数必须大于 0"));if(entry.Value is GraphFieldOverride ge&&long.TryParse(ge.EdgeCount,out var en)&&en<0)result.Add(new($"groups.{group.Name}.{entry.Key}","图边数不能为负数"));if(entry.Value is GraphFieldOverride gx&&long.TryParse(gx.VertexCount,out var xv)&&long.TryParse(gx.EdgeCount,out var xe)){var target=c.Fields.FirstOrDefault(f=>f.Name==entry.Key) as GraphField;if(target is not null&&!target.AllowMultiEdges){var max=target.AllowSelfLoops?(target.Directed?xv*xv:xv*(xv+1)/2):(target.Directed?xv*Math.Max(0,xv-1):xv*Math.Max(0,xv-1)/2);if(xe>max)result.Add(new($"groups.{group.Name}.{entry.Key}",$"Subtask 图边数超过最大值 {max}"));}}}}var ordered=c.Groups.OrderBy(x=>x.From).ToList();for(var i=1;i<ordered.Count;i++)if(ordered[i].From<=ordered[i-1].To)result.Add(new("groups","测试点分组存在重叠"));return result;}

 /// <summary>逐个检查字段里所有「参数表达式」是否为空。空文本无法求值，必须在校验阶段拦下。</summary>
 static IEnumerable<ValidationIssue> FieldExpressionIssues(FieldConfig f)=>f switch
 {
  IntField x => Required(x.Name,"最小值",x.Min).Concat(Required(x.Name,"最大值",x.Max)),
  FloatField x => Required(x.Name,"最小值",x.Min).Concat(Required(x.Name,"最大值",x.Max)),
  StringField x => Required(x.Name,"固定长度",x.Length).Concat(Optional(x.Name,"最小长度",x.MinLength)).Concat(Optional(x.Name,"最大长度",x.MaxLength)),
  ArrayField x => Required(x.Name,"数组长度",x.Length).Concat(Required(x.Name,"元素最小值",x.Min)).Concat(Required(x.Name,"元素最大值",x.Max)),
  TreeField x => Required(x.Name,"节点数",x.Nodes).Concat(Required(x.Name,"权值下界",x.WeightMin)).Concat(Required(x.Name,"权值上界",x.WeightMax)),
  GraphField x => Required(x.Name,"节点数",x.Nodes).Concat(Required(x.Name,"边数",x.Edges)).Concat(Required(x.Name,"权值下界",x.WeightMin)).Concat(Required(x.Name,"权值上界",x.WeightMax)),
  _ => Enumerable.Empty<ValidationIssue>()
 };

 static IEnumerable<ValidationIssue> Required(string field,string label,string? text)
  => DataMaker.Refactored.Expressions.ExpressionEvaluator.IsBlank(text)
   ? new[]{ new ValidationIssue($"fields.{field}",$"{label}不能为空") }
   : Enumerable.Empty<ValidationIssue>();

 static IEnumerable<ValidationIssue> Optional(string field,string label,string? text)
  => text is not null && string.IsNullOrWhiteSpace(text)
   ? new[]{ new ValidationIssue($"fields.{field}",$"{label}要么留空要么填写表达式") }
   : Enumerable.Empty<ValidationIssue>();

 static IEnumerable<ValidationIssue> ExpressionPairIssues(string field,string min,string max)
 {
  if(!double.TryParse(min,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var lo)) yield break;
  if(!double.TryParse(max,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var hi)) yield break;
  if(hi<lo) yield return new ValidationIssue($"fields.{field}","浮点范围无效");
 }

 /// <summary>字段所有「参数表达式」文本。</summary>
 public static IEnumerable<string> ExpressionTexts(FieldConfig f)=>f switch
 {
  IntField x => new[]{ x.Min, x.Max },
  FloatField x => new[]{ x.Min, x.Max },
  StringField x => new[]{ x.Length, x.MinLength ?? string.Empty, x.MaxLength ?? string.Empty },
  ArrayField x => new[]{ x.Length, x.Min, x.Max },
  TreeField x => new[]{ x.Nodes, x.WeightMin, x.WeightMax },
  GraphField x => new[]{ x.Nodes, x.Edges, x.WeightMin, x.WeightMax },
  _ => Enumerable.Empty<string>()
 };

 /// <summary>字段参数表达式里引用到的变量名（按表达式标识符解析，而不是对整段 JSON 做子串匹配）。</summary>
 public static IReadOnlyList<string> ReferencedVariables(FieldConfig f)
  => ExpressionTexts(f).SelectMany(Identifiers).Distinct().ToList();

 static IEnumerable<string> Identifiers(string? text)=>DataMaker.Refactored.Expressions.ExpressionEvaluator.Identifiers(text);
}
