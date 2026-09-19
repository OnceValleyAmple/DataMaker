using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataMaker;

public static class ConfigStore
{
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public static void Save(ProjectConfig c, string path) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(c, Options)); }
    public static ProjectConfig Load(string path) => JsonSerializer.Deserialize<ProjectConfig>(File.ReadAllText(path), Options) ?? new();
}

public sealed class ExpressionException : Exception { public ExpressionException(string message) : base(message) { } }
public static class ExpressionEvaluator
{
    public static long Evaluate(string expression, IReadOnlyDictionary<string, long> variables)
    {
        var p = new Parser(expression, variables); var value = p.ParseExpression(); p.Skip();
        if (!p.End) throw new ExpressionException($"表达式存在多余内容：{expression[p.Position..]}");
        return value;
    }
    sealed class Parser
    {
        readonly string s; readonly IReadOnlyDictionary<string,long> vars; public int Position; public bool End { get { Skip(); return Position >= s.Length; } }
        public Parser(string s,IReadOnlyDictionary<string,long> v){this.s=s;vars=v;}
        public void Skip(){while(Position<s.Length&&char.IsWhiteSpace(s[Position]))Position++;}
        public long ParseExpression(){var x=ParseTerm();while(true){Skip();if(Match('+'))x=checked(x+ParseTerm());else if(Match('-'))x=checked(x-ParseTerm());else return x;}}
        long ParseTerm(){var x=ParseFactor();while(true){Skip();if(Match('*'))x=checked(x*ParseFactor());else if(Match('/')){var y=ParseFactor();if(y==0)throw new ExpressionException("表达式除数不能为零");x/=y;}else if(Match('%')){var y=ParseFactor();if(y==0)throw new ExpressionException("表达式取模不能为零");x%=y;}else return x;}}
        long ParseFactor(){Skip();if(Match('-'))return checked(-ParseFactor());if(Match('(')){var x=ParseExpression();if(!Match(')'))throw new ExpressionException("缺少右括号");return x;}if(Position>=s.Length)throw new ExpressionException("表达式不完整");if(char.IsDigit(s[Position])){var start=Position;while(Position<s.Length&&char.IsDigit(s[Position]))Position++;return long.Parse(s[start..Position]);}if(char.IsLetter(s[Position])||s[Position]=='_'){var start=Position++;while(Position<s.Length&&(char.IsLetterOrDigit(s[Position])||s[Position]=='_'))Position++;var name=s[start..Position];if(!vars.TryGetValue(name,out var x))throw new ExpressionException($"变量未定义：{name}");return x;}throw new ExpressionException($"无法识别字符：{s[Position]}");}
        bool Match(char c){Skip();if(Position<s.Length&&s[Position]==c){Position++;return true;}return false;}
    }
}

public sealed record Edge(int U, int V, long Weight = 0);
public static class StructureGenerator
{
    public static List<Edge> Tree(int n, string shape, Random r, long minWeight=1, long maxWeight=100)
    { if(n<1) throw new ArgumentException("树节点数必须大于 0"); var e=new List<Edge>(); for(int v=2;v<=n;v++){int p=shape switch {"chain"=>v-1,"star"=>1,"binary"=>Math.Max(1,(v-1)/2),_=>r.Next(1,v)};e.Add(new(p,v,Weight(r,minWeight,maxWeight)));}return e; }
    public static List<Edge> Graph(int n,int m,bool directed,bool connected,bool dag,bool bipartite,bool allowSelf,bool allowMulti,Random r,long minWeight=1,long maxWeight=100)
    { if(n<1||m<0)throw new ArgumentException("图参数无效");long max=allowMulti?long.MaxValue:(long)n*n-(directed?0:n);if(!allowSelf)max-=directed?n:n; if(m>max&& !allowMulti)throw new ArgumentException("边数超过约束");var list=new List<Edge>();var set=new HashSet<(int,int)>();void Add(int u,int v){if(!allowSelf&&u==v)return;if(dag&&u>=v)return;if(bipartite&&((u&1)==(v&1)))return;var key=directed?(u,v):(Math.Min(u,v),Math.Max(u,v));if(!allowMulti&&!set.Add(key))return;list.Add(new(u,v,Weight(r,minWeight,maxWeight)));}if(connected&&n>1){for(int v=2;v<=n;v++){var p=r.Next(1,v);if(bipartite)p=(v%2==0)?1:2; if(dag&&p>=v)p=v-1;Add(p,v);}}int tries=0;while(list.Count<m&&tries++<m*100+1000){int u=r.Next(1,n+1),v=r.Next(1,n+1);if(dag&&u>v)(u,v)=(v,u);Add(u,v);}if(list.Count<m)throw new ArgumentException("无法在当前约束下生成足够的边");return list;}
    static long Weight(Random r,long a,long b)=>a>=b?a:a+(long)(r.NextDouble()*(b-a+1));
}

public static class DataGenerator
{
 public static string Generate(ProjectConfig c,int seed,int testPoint=1){if(c.MultiGroupMode){var groupRandom=new Random(seed);var t=groupRandom.Next(c.GroupCountMin,c.GroupCountMax+1);var single=new ProjectConfig{Fields=c.Fields,Groups=c.Groups,MultiGroupMode=false};var blocks=new List<string>{t.ToString()};for(var g=0;g<t;g++)blocks.Add(Generate(single,seed+g+1,testPoint));return string.Join(Environment.NewLine,blocks)+Environment.NewLine;}var r=new Random(seed);var vars=new Dictionary<string,long>();var lines=new List<string>();foreach(var original in c.Fields){var f=CloneWithOverride(original,c.Groups.FirstOrDefault(g=>testPoint>=g.From&&testPoint<=g.To));switch(f.Type){case FieldType.Float:var flo=double.Parse(f.Min,System.Globalization.CultureInfo.InvariantCulture);var fhi=double.Parse(f.Max,System.Globalization.CultureInfo.InvariantCulture);if(fhi<flo)throw new ExpressionException($"字段 {f.Name} 的范围无效");var fv=flo+r.NextDouble()*(fhi-flo);lines.Add(fv.ToString($"F{Math.Max(0,f.Precision)}",System.Globalization.CultureInfo.InvariantCulture));break;case FieldType.Int:var lo=ExpressionEvaluator.Evaluate(f.Min,vars);var hi=ExpressionEvaluator.Evaluate(f.Max,vars);if(hi<lo)throw new ExpressionException($"字段 {f.Name} 的范围无效");var v=lo+r.NextInt64(0,hi-lo+1);vars[f.Name]=v;lines.Add(v.ToString());break;case FieldType.Array:var n=ExpressionEvaluator.Evaluate(f.Length,vars);var values=Enumerable.Range(0,Math.Max(0,(int)n)).Select(_=>Next(r,f.Min,f.Max,vars)).ToList();
                if (f.ElementType == "float") { var fl=double.Parse(f.Min,System.Globalization.CultureInfo.InvariantCulture); var fh=double.Parse(f.Max,System.Globalization.CultureInfo.InvariantCulture); lines.Add(string.Join(" ",Enumerable.Range(0,Math.Max(0,(int)n)).Select(_=>(fl+r.NextDouble()*(fh-fl)).ToString($"F{f.Precision}",System.Globalization.CultureInfo.InvariantCulture)))); break; }
                if (f.ElementType == "string") { var arrayChars=string.IsNullOrEmpty(f.Charset)?"abcdefghijklmnopqrstuvwxyz":f.Charset; lines.Add(string.Join(" ",Enumerable.Range(0,Math.Max(0,(int)n)).Select(_=>new string(Enumerable.Range(0,Math.Max(0,(int)ExpressionEvaluator.Evaluate(f.Max,vars))).Select(__=>arrayChars[r.Next(arrayChars.Length)]).ToArray())))); break; }if(f.Unique){values=values.Distinct().ToList();var attempts=0;while(values.Count<n&&attempts++<n*20) { var candidate=Next(r,f.Min,f.Max,vars);if(!values.Contains(candidate))values.Add(candidate); }if(values.Count<n)throw new ExpressionException($"字段 {f.Name} 的去重范围不足以生成 {n} 个元素");}if(f.SortOrder=="asc")values.Sort();else if(f.SortOrder=="desc"){values.Sort();values.Reverse();}lines.Add(f.Output=="one-per-line"?string.Join("\n",values):string.Join(" ",values));break;case FieldType.String:var len=(int)ExpressionEvaluator.Evaluate(f.Length,vars);var stringChars=string.IsNullOrEmpty(f.Charset)?"abcdefghijklmnopqrstuvwxyz":f.Charset;var text=new string(Enumerable.Range(0,len).Select(_=>stringChars[r.Next(stringChars.Length)]).ToArray());if(f.StringPattern=="palindrome"){var half=new string(text.Take((len+1)/2).ToArray());text=half+new string(half.Reverse().Skip(len%2).ToArray());}else if(f.StringPattern=="same"&&stringChars.Length>0)text=new string(stringChars[r.Next(stringChars.Length)],len);else if(f.StringPattern=="periodic"&&stringChars.Length>0)text=new string(Enumerable.Range(0,len).Select(i=>stringChars[i%stringChars.Length]).ToArray());lines.Add(text);break;case FieldType.Tree:var te=StructureGenerator.Tree((int)ExpressionEvaluator.Evaluate(f.Length,vars),f.Structure,r);lines.Add(string.Join("\n",te.Select(x=>f.Weighted?$"{x.U} {x.V} {x.Weight}":$"{x.U} {x.V}")));break;case FieldType.Graph:var ge=StructureGenerator.Graph((int)ExpressionEvaluator.Evaluate(f.Length,vars),int.Parse(f.Max),f.Directed || f.Structure=="directed" || f.Structure=="dag",f.Connected,f.Structure=="dag",f.Bipartite,f.AllowSelfLoop,f.AllowMultiEdge,r);lines.Add(string.Join("\n",ge.Select(x=>f.Weighted?$"{x.U} {x.V} {x.Weight}":$"{x.U} {x.V}")));break;default:throw new NotSupportedException($"字段类型暂不支持：{f.Type}");}}return string.Join(Environment.NewLine,lines)+Environment.NewLine;}
 static long Next(Random r,string min,string max,IReadOnlyDictionary<string,long> vars){var a=ExpressionEvaluator.Evaluate(min,vars);var b=ExpressionEvaluator.Evaluate(max,vars);if(b<a)throw new ExpressionException("随机范围无效");return a+r.NextInt64(0,b-a+1);}
 static FieldConfig CloneWithOverride(FieldConfig f,GroupOverride? g){var x=new FieldConfig{Name=f.Name,Type=f.Type,Min=f.Min,Max=f.Max,Length=f.Length,ElementType=f.ElementType,Charset=f.Charset,StringPattern=f.StringPattern,Output=f.Output,Structure=f.Structure,Weighted=f.Weighted,Precision=f.Precision,Unique=f.Unique,SortOrder=f.SortOrder,Directed=f.Directed,Connected=f.Connected,AllowSelfLoop=f.AllowSelfLoop,AllowMultiEdge=f.AllowMultiEdge,Bipartite=f.Bipartite,LeftPartSize=f.LeftPartSize};if(g is not null&&g.Overrides.TryGetValue(f.Name,out var value)){if(value.Contains("=")){foreach(var part in value.Split(';')){var p=part.Split('=',2);if(p.Length!=2)continue;if(p[0].Trim().Equals("min",StringComparison.OrdinalIgnoreCase))x.Min=p[1].Trim();if(p[0].Trim().Equals("max",StringComparison.OrdinalIgnoreCase))x.Max=p[1].Trim();if(p[0].Trim().Equals("length",StringComparison.OrdinalIgnoreCase))x.Length=p[1].Trim();}}}return x;}
}

public sealed record CompileResult(bool Success,string ExePath,string Output);
public static class CompilerService
{
 public static string? FindGpp(){var candidates=new[]{Environment.GetEnvironmentVariable("DATAMAKER_GPP"),@"C:\mingw64\bin\g++.exe",@"C:\msys64\mingw64\bin\g++.exe"}.Where(x=>!string.IsNullOrWhiteSpace(x));foreach(var x in candidates)if(File.Exists(x))return x;foreach(var d in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')){var p=Path.Combine(d.Trim(),'g'+'+'+'+'+".exe");if(File.Exists(p))return p;}return null;}
 public static async Task<CompileResult> CompileAsync(string gpp,string source,string args,string output,int timeoutMs=30000){var psi=new ProcessStartInfo(gpp,$"\"{source}\" {args} -o \"{output}\""){UseShellExecute=false,RedirectStandardError=true,RedirectStandardOutput=true,CreateNoWindow=true};using var p=Process.Start(psi)??throw new InvalidOperationException("无法启动 g++");var text=await p.StandardError.ReadToEndAsync();await p.WaitForExitAsync();return new(p.ExitCode==0&&File.Exists(output),output,text);}
}
public sealed record RunResult(bool Success,string Output,string Error);
public static class StandardRunner
{
 public static async Task<RunResult> RunAsync(string exe,string input,int timeoutMs,CancellationToken ct,int memoryLimitMb=0){var psi=new ProcessStartInfo(exe){UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};using var p=Process.Start(psi)??throw new InvalidOperationException("无法启动标程");using var job=memoryLimitMb>0?JobObject.Create(memoryLimitMb):null;if(job is not null&&!job.Assign(p))return new(false,"","无法设置进程资源限制");await p.StandardInput.WriteAsync(input);p.StandardInput.Close();var wait=p.WaitForExitAsync(ct);if(await Task.WhenAny(wait,Task.Delay(timeoutMs,ct))!=wait){try{job?.Kill();p.Kill(true);}catch{}return new(false,"","运行超时");}var output=await p.StandardOutput.ReadToEndAsync(ct);var error=await p.StandardError.ReadToEndAsync(ct);if(p.ExitCode!=0)return new(false,output,$"程序退出码：{p.ExitCode}\n{error}");if(string.IsNullOrWhiteSpace(output))return new(false,output,"标程输出为空");return new(true,output,error);}
 public static void Pack(string dir,string zip){if(File.Exists(zip))File.Delete(zip);using var archive=ZipFile.Open(zip,ZipArchiveMode.Create);foreach(var file in Directory.EnumerateFiles(dir,"*",SearchOption.TopDirectoryOnly)){if(string.Equals(Path.GetFileName(file),"solution.exe",StringComparison.OrdinalIgnoreCase))continue;archive.CreateEntryFromFile(file,Path.GetFileName(file),CompressionLevel.Optimal);}}
}

internal sealed class JobObject : IDisposable
{
 [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)] static extern nint CreateJobObject(nint a,string? name);
 [System.Runtime.InteropServices.DllImport("kernel32.dll",SetLastError=true)] static extern bool SetInformationJobObject(nint h,int info,ref Limits data,uint length);
 [System.Runtime.InteropServices.DllImport("kernel32.dll",SetLastError=true)] static extern bool AssignProcessToJobObject(nint job,nint process);
 [System.Runtime.InteropServices.DllImport("kernel32.dll",SetLastError=true)] static extern bool TerminateJobObject(nint job,uint code);
 [System.Runtime.InteropServices.DllImport("kernel32.dll",SetLastError=true)] static extern bool CloseHandle(nint h);
 const int Extended=9; const uint JobLimitProcessMemory=0x100; nint handle;
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] struct Basic{public ulong PerProcessUserTimeLimit;public ulong PerJobUserTimeLimit;public uint LimitFlags;public nuint MinimumWorkingSetSize;public nuint MaximumWorkingSetSize;public uint ActiveProcessLimit;public nuint Affinity;public uint PriorityClass;public uint SchedulingClass;}
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] struct Io{public ulong Read;public ulong Write;public ulong Other;}
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] struct Limits{public Basic Basic;public Io Io;public ulong ProcessMemoryLimit;public ulong JobMemoryLimit;public ulong PeakProcessMemoryUsed;public ulong PeakJobMemoryUsed;}
 public static JobObject? Create(int mb){var j=new JobObject{handle=CreateJobObject(0,null)};if(j.handle==0)return null;var l=new Limits{Basic=new Basic{LimitFlags=JobLimitProcessMemory},ProcessMemoryLimit=(ulong)mb*1024*1024};if(!SetInformationJobObject(j.handle,Extended,ref l,(uint)System.Runtime.InteropServices.Marshal.SizeOf<Limits>())){j.Dispose();return null;}return j;}
 public bool Assign(Process p)=>AssignProcessToJobObject(handle,p.Handle);
 public void Kill()=>TerminateJobObject(handle,1);
 public void Dispose(){if(handle!=0)CloseHandle(handle);handle=0;}
}
