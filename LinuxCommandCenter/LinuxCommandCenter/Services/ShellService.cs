using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinuxCommandCenter.Models;

namespace LinuxCommandCenter.Services;

public class ShellService
{
    public event EventHandler<CommandResult>? CommandExecuted;

    public async Task<CommandResult> RunCommandAsync(
        string command,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command must not be empty.", nameof(command));

        var (fileName, arguments) = BuildShellCommand(command);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        var result = new CommandResult
        {
            CommandText = command,
            WorkingDirectory = startInfo.WorkingDirectory ?? string.Empty,
            StartTime = DateTimeOffset.Now
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var stdOutBuilder = new StringBuilder();
        var stdErrBuilder = new StringBuilder();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                lock (stdOutBuilder)
                {
                    stdOutBuilder.AppendLine(e.Data);
                }
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                lock (stdErrBuilder)
                {
                    stdErrBuilder.AppendLine(e.Data);
                }
            }
        };

        process.Exited += (_, _) =>
        {
            tcs.TrySetResult(true);
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start shell process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // ignore
                }
            }))
            {
                await tcs.Task.ConfigureAwait(false);
            }

            // Small delay to ensure all async output has been read
            await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);

            result.ExitCode = process.ExitCode;
            result.StdOutput = stdOutBuilder.ToString();
            result.StdError = stdErrBuilder.ToString();
            result.EndTime = DateTimeOffset.Now;

            CommandExecuted?.Invoke(this, result);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTimeOffset.Now;
            result.ExitCode = -1;
            result.StdError = ex.ToString();
            CommandExecuted?.Invoke(this, result);
            return result;
        }
    }

    private static (string FileName, string Arguments) BuildShellCommand(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ("cmd.exe", "/C " + command);
        }

        // Linux / macOS: use bash
        return ("/bin/bash", "-c \"" + command.Replace("\"", "\\\"") + "\"");
    }
}