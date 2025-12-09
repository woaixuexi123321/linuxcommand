using System;

namespace LinuxCommandCenter.Models;

public class CommandResult
{
    public string CommandText { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string StdOutput { get; set; } = string.Empty;
    public string StdError { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
    public bool IsSuccess => ExitCode == 0;

    public string Summary =>
        $"{StartTime:HH:mm:ss} • Exit {ExitCode} • {(IsSuccess ? "Success" : "Error")}";
}