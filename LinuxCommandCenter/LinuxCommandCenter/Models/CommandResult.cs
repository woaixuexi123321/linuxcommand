using System;

namespace LinuxCommandCenter.Models
{
    public class CommandResult
    {
        public string Command { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccess => ExitCode == 0;
        public TimeSpan ExecutionTime { get; set; }
    }
}