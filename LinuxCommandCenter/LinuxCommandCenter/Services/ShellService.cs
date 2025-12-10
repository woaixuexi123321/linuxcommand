using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LinuxCommandCenter.Models;

namespace LinuxCommandCenter.Services
{
    public class ShellService
    {
        public async Task<CommandResult> ExecuteCommandAsync(string command, bool useSudo = false, string? workingDirectory = null)
        {
            var result = new CommandResult
            {
                Command = command,
                Timestamp = DateTime.Now
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var escapedArgs = command.Replace("\"", "\\\"");

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = useSudo ? "sudo" : "/bin/bash",
                        Arguments = useSudo ? $"-S bash -c \"{escapedArgs}\"" : $"-c \"{escapedArgs}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    }
                };

                process.Start();

                if (useSudo)
                {
                    // In a real application, you would securely get the password
                    // For now, we'll assume sudo doesn't require password or it's configured with NOPASSWD
                    await process.StandardInput.WriteLineAsync();
                    await process.StandardInput.FlushAsync();
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, process.WaitForExitAsync());

                stopwatch.Stop();

                result.Output = await outputTask;
                result.Error = await errorTask;
                result.ExitCode = process.ExitCode;
                result.ExecutionTime = stopwatch.Elapsed;
            }
            catch (Exception ex)
            {
                result.Error = $"Error executing command: {ex.Message}";
                result.ExitCode = -1;
            }

            return result;
        }

        public async Task<CommandResult> TestConnectionAsync()
        {
            return await ExecuteCommandAsync("echo 'Connection Test Successful' && whoami && hostname");
        }

        public async Task<CommandResult> GetSystemInfoAsync()
        {
            return await ExecuteCommandAsync("uname -a && lsb_release -a 2>/dev/null || cat /etc/os-release");
        }

        public async Task<CommandResult> GetDiskUsageAsync()
        {
            return await ExecuteCommandAsync("df -h");
        }

        public async Task<CommandResult> GetProcessListAsync()
        {
            return await ExecuteCommandAsync("ps aux --sort=-%cpu | head -20");
        }
    }
}