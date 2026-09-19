namespace DataMaker.Refactored.Models;

public enum TestPointStatus
{
    Pending,
    Generating,
    Running,
    Success,
    Skipped,
    Failed,
    Timeout,
    Crashed,
    EmptyOutput
}

public sealed class TestPointResult
{
    public int Index { get; init; }
    public TestPointStatus Status { get; set; }
    public string StatusText => Status switch { TestPointStatus.Pending => "等待中", TestPointStatus.Generating => "生成中", TestPointStatus.Running => "运行中", TestPointStatus.Success => "成功", TestPointStatus.Skipped => "已跳过", TestPointStatus.Failed => "失败", TestPointStatus.Timeout => "超时", TestPointStatus.Crashed => "崩溃", TestPointStatus.EmptyOutput => "输出为空", _ => "未知" };
    public TimeSpan Duration { get; set; }
    public double DurationMs => Duration.TotalMilliseconds;
    public string InputPath { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public string Error { get; set; } = "";
}
