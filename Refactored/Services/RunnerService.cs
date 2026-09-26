using System.Text;

namespace DataMaker.Refactored.Services;

public sealed record RunResult(bool Success, string Output, string Error, TimeSpan Duration);

public sealed class RunnerService
{
    const int ReadBufferBytes = 64 * 1024;

    public async Task<RunResult> RunAsync(
        string executable,
        string input,
        int timeoutMs,
        int memoryLimitMb = 0,
        int maxOutputMb = 64,
        CancellationToken token = default)
    {
        var start = DateTime.UtcNow;
        var psi = new System.Diagnostics.ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var p = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException("无法启动标程");
        using var job = JobObjectService.TryCreate(memoryLimitMb);
        if (job is not null && !job.TryAssign(p)) job.Dispose();

        await p.StandardInput.WriteAsync(input);
        p.StandardInput.Close();

        var outputLimitBytes = maxOutputMb > 0 ? maxOutputMb * 1024L * 1024L : long.MaxValue;
        var outputTask = ReadBoundedAsync(p.StandardOutput.BaseStream, outputLimitBytes, token);
        var errorTask = p.StandardError.ReadToEndAsync(token);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(timeoutMs);

        try
        {
            await p.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { job?.Kill(); p.Kill(true); } catch { }
            return new(false, "", token.IsCancellationRequested ? "已中止" : "运行超时", DateTime.UtcNow - start);
        }

        BoundedOutput captured;
        string error;
        try
        {
            captured = await outputTask;
            error = await errorTask;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            try { job?.Kill(); p.Kill(true); } catch { }
            return new(false, "", "读取标程输出失败：" + ex.Message, DateTime.UtcNow - start);
        }

        if (captured.Truncated)
        {
            try { job?.Kill(); p.Kill(true); } catch { }
            return new(false, "", "标程输出超过限制", DateTime.UtcNow - start);
        }

        var output = Encoding.UTF8.GetString(captured.Bytes, 0, captured.Count);

        if (p.ExitCode != 0) return new(false, output, $"退出码：{p.ExitCode}\n{error}", DateTime.UtcNow - start);
        if (string.IsNullOrWhiteSpace(output)) return new(false, output, "标程输出为空", DateTime.UtcNow - start);
        return new(true, output, error, DateTime.UtcNow - start);
    }

    /// <summary>
    /// 边读边累计字节数，一旦超过上限立刻停止读取（调用方随后终止进程），
    /// 避免把超大输出完整读进内存之后才发现超限。
    /// </summary>
    static async Task<BoundedOutput> ReadBoundedAsync(Stream stream, long limitBytes, CancellationToken token)
    {
        var buffer = new byte[ReadBufferBytes];
        var data = new MemoryStream();
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, token)) > 0)
        {
            total += read;
            if (total > limitBytes) return new BoundedOutput(data.ToArray(), (int)data.Length, true);
            data.Write(buffer, 0, read);
        }
        return new BoundedOutput(data.ToArray(), (int)data.Length, false);
    }

    readonly record struct BoundedOutput(byte[] Bytes, int Count, bool Truncated);
}
