namespace DataMaker.Refactored.Services;
public sealed record CompileResult(bool Success,string Executable,string Output);
public sealed class CompilerService
{
 public static string? FindGpp(string? configured=null){var candidates=new[]{configured,@"C:\mingw64\bin\g++.exe",@"C:\msys64\mingw64\bin\g++.exe"}.Where(x=>!string.IsNullOrWhiteSpace(x));foreach(var c in candidates)if(File.Exists(c))return c;foreach(var d in(Environment.GetEnvironmentVariable("PATH")??"").Split(';')){var p=Path.Combine(d,"g++.exe");if(File.Exists(p))return p;}return null;}
 public async Task<CompileResult> CompileAsync(string compiler,string source,string args,string output,int timeoutMs=30000,CancellationToken token=default){var psi=new System.Diagnostics.ProcessStartInfo(compiler,$"\"{source}\" {args} -o \"{output}\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};using var p=System.Diagnostics.Process.Start(psi)??throw new InvalidOperationException("无法启动 g++");var text=await p.StandardError.ReadToEndAsync(token);using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(timeoutMs);try{await p.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){try{p.Kill(true);}catch{}return new(false,output,"编译超时");}return new(p.ExitCode==0&&File.Exists(output),output,text);}
}
