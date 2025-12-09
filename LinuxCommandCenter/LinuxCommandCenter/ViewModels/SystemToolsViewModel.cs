using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels;

public class SystemToolsViewModel : ViewModelBase
{
    private readonly ShellService _shellService;
    private readonly Dictionary<string, (string Title, string Command)> _tools;

    private string _activeToolTitle = "No tool selected";
    private string _output = "Choose a tool to see its output.";
    private string _status = "Ready";
    private bool _isBusy;

    public SystemToolsViewModel(ShellService shellService)
    {
        _shellService = shellService;

        _tools = new Dictionary<string, (string Title, string Command)>(StringComparer.OrdinalIgnoreCase)
        {
            ["sys-summary"] = (
                "System summary",
                "uname -a && echo && (lsb_release -a 2>/dev/null || cat /etc/os-release)"),
            ["disk-usage"] = (
                "Disk usage",
                "df -h"),
            ["memory"] = (
                "Memory usage",
                "free -h"),
            ["top-processes"] = (
                "Top processes (CPU)",
                "ps aux --sort=-%cpu | head -n 20"),
            ["services"] = (
                "Running services (systemd)",
                "systemctl list-units --type=service --state=running"),
            ["network"] = (
                "Network overview",
                "ip addr && echo && ip route"),
            ["journal"] = (
                "Journal (last 200 lines)",
                "journalctl -n 200 --no-pager")
        };

        RunToolCommand = new AsyncRelayCommand<string>(RunToolAsync, CanRunTool);
    }

    public string ActiveToolTitle
    {
        get => _activeToolTitle;
        set => SetProperty(ref _activeToolTitle, value);
    }

    public string Output
    {
        get => _output;
        set => SetProperty(ref _output, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RunToolCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand<string> RunToolCommand { get; }

    private bool CanRunTool(string? key) => !IsBusy && !string.IsNullOrWhiteSpace(key);

    private async Task RunToolAsync(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!_tools.TryGetValue(key, out var info))
        {
            Status = "Unknown tool.";
            return;
        }

        IsBusy = true;
        ActiveToolTitle = info.Title;
        Status = "Running...";
        Output = string.Empty;

        var result = await _shellService.RunCommandAsync(info.Command);

        Output = string.IsNullOrWhiteSpace(result.StdOutput)
            ? "(no standard output)"
            : result.StdOutput;

        if (!string.IsNullOrWhiteSpace(result.StdError))
        {
            Output += Environment.NewLine + Environment.NewLine +
                      "=== Error output ===" + Environment.NewLine +
                      result.StdError;
        }

        Status = result.IsSuccess
            ? $"Completed in {result.Duration.TotalSeconds:F2}s (exit code {result.ExitCode})."
            : $"Failed in {result.Duration.TotalSeconds:F2}s (exit code {result.ExitCode}).";

        IsBusy = false;
    }
}