using DataMaker.Refactored.Models;

namespace DataMaker.Refactored.Services;

public sealed record PipelineResult(
    bool Success,
    string ZipPath,
    IReadOnlyList<TestPointResult> TestPoints,
    IReadOnlyList<string> Messages);

public sealed class PipelineService
{
    readonly GenerationService generation = new();
    readonly CompilerService compiler = new();
    readonly RunnerService runner = new();
    readonly ZipService zipper = new();

    public async Task<PipelineResult> ExecuteAsync(
        ProjectConfig config,
        IProgress<(int, string)>? progress = null,
        CancellationToken token = default)
    {
        var points = new List<TestPointResult>();
        var messages = new List<string>();
        var issues = ValidationService.Validate(config);

        if (issues.Count > 0)
            return new(false, "", points, issues.Select(x => $"{x.Path}: {x.Message}").ToList());

        var work = Path.Combine(Path.GetTempPath(), "DataMaker_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);

        try
        {
            var gpp = CompilerService.FindGpp(config.MingwPath);
            if (gpp is null)
                return new(false, "", points, new[] { "未找到 g++.exe" });

            var exe = Path.Combine(work, "solution.exe");
            var compiled = await compiler.CompileAsync(
                gpp, config.SourcePath, config.CompileArgs, exe, 30000, token);

            if (!compiled.Success)
                return new(false, "", points, new[] { "编译失败：" + compiled.Output });

            await generation.GenerateAsync(config, work, progress, token);

            for (var i = 1; i <= config.TestCount; i++)
            {
                token.ThrowIfCancellationRequested();

                var point = new TestPointResult
                {
                    Index = i,
                    Status = TestPointStatus.Running,
                    InputPath = Path.Combine(work, $"{i}.in"),
                    OutputPath = Path.Combine(work, $"{i}.out")
                };

                points.Add(point);
                var start = DateTime.UtcNow;

                try
                {
                    var input = await File.ReadAllTextAsync(point.InputPath, token);
                    RunResult result;
                    var attempt = 0;

                    do
                    {
                        result = await runner.RunAsync(
                            exe,
                            input,
                            config.TimeLimitMs,
                            config.MemoryLimitMb,
                            config.MaxOutputMb,
                            token);
                        attempt++;
                    }
                    while (!result.Success && attempt <= config.RetryCount);

                    point.Duration = DateTime.UtcNow - start;
                    point.Error = result.Error;
                    point.Status = result.Success
                        ? TestPointStatus.Success
                        : result.Error.Contains("超时")
                            ? TestPointStatus.Timeout
                            : result.Error.Contains("为空")
                                ? TestPointStatus.EmptyOutput
                                : TestPointStatus.Failed;

                    await File.WriteAllTextAsync(point.OutputPath, result.Output, token);
                    messages.Add($"测试点 {i}: {(result.Success ? "完成" : result.Error)}");

                    if (!result.Success && config.FailurePolicy == FailurePolicy.Stop)
                    {
                        messages.Add($"测试点 {i} 失败，按策略停止后续测试点。");
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    point.Duration = DateTime.UtcNow - start;
                    point.Status = TestPointStatus.Skipped;
                    point.Error = "已中止";
                    throw;
                }
                catch (Exception ex)
                {
                    point.Duration = DateTime.UtcNow - start;
                    point.Status = TestPointStatus.Failed;
                    point.Error = ex.Message;
                    messages.Add($"测试点 {i}: {ex.Message}");

                    if (config.FailurePolicy == FailurePolicy.Stop)
                        break;
                }
            }

            zipper.Create(work, config.ZipPath);
            return new(true, config.ZipPath, points, messages);
        }
        catch (OperationCanceledException)
        {
            return new(false, "", points, messages.Append("已中止").ToList());
        }
        catch (Exception ex)
        {
            return new(false, "", points, messages.Append(ex.Message).ToList());
        }
    }
}
