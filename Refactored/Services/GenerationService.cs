using DataMaker.Refactored.Generators; using DataMaker.Refactored.Models;
namespace DataMaker.Refactored.Services;
public sealed class GenerationService
{
 readonly GeneratorService generator=new();
 public async Task GenerateAsync(ProjectConfig config,string directory,IProgress<(int,string)>? progress=null,CancellationToken cancellationToken=default){var issues=ValidationService.Validate(config);if(issues.Count>0)throw new InvalidOperationException(string.Join(Environment.NewLine,issues.Select(x=>$"{x.Path}: {x.Message}")));Directory.CreateDirectory(directory);for(var i=1;i<=config.TestCount;i++){cancellationToken.ThrowIfCancellationRequested();var input=GenerateTest(config,i,Environment.TickCount+i);await File.WriteAllTextAsync(Path.Combine(directory,$"{i}.in"),input,cancellationToken);progress?.Report((i,"输入已生成"));}}
 string GenerateTest(ProjectConfig config,int point,int seed){if(!config.MultiGroup)return generator.Generate(config,point,seed);var random=new Random(seed);var count=random.Next(config.GroupCountMin,config.GroupCountMax+1);var one=new ProjectConfig{Version=config.Version,Name=config.Name,TestCount=config.TestCount,TimeLimitMs=config.TimeLimitMs,MemoryLimitMb=config.MemoryLimitMb,MingwPath=config.MingwPath,SourcePath=config.SourcePath,CompileArgs=config.CompileArgs,ZipPath=config.ZipPath,MultiGroup=false,Fields=config.Fields,Groups=config.Groups};var blocks=new List<string>{count.ToString()};for(var i=0;i<count;i++)blocks.Add(generator.Generate(one,point,seed+i+1));return string.Join(Environment.NewLine,blocks)+Environment.NewLine;}
}
