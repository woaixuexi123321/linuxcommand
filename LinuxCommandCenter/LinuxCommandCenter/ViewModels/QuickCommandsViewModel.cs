using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Models;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels;

public class QuickCommandsViewModel : ViewModelBase
{
    private readonly ShellService _shellService;

    private CommandPreset? _selectedPreset;
    private string _additionalArguments = string.Empty;
    private string _output = string.Empty;
    private string _error = string.Empty;
    private string _status = "Ready";
    private bool _isBusy;

    public QuickCommandsViewModel(ShellService shellService)
    {
        _shellService = shellService;

        Presets = new ObservableCollection<CommandPreset>
        {
            new CommandPreset(
                name: "List processes",
                category: "Monitoring",
                commandTemplate: "ps aux --sort=-%mem | head -n 20",
                description: "Show the top 20 processes by memory usage."),
            new CommandPreset(
                name: "Disk usage",
                category: "Storage",
                commandTemplate: "df -h",
                description: "Display disk usage for all mounted file systems."),
            new CommandPreset(
                name: "Memory usage",
                category: "Memory",
                commandTemplate: "free -h",
                description: "Show total, used and free memory."),
            new CommandPreset(
                name: "Network interfaces",
                category: "Network",
                commandTemplate: "ip addr",
                description: "List all network interfaces and their addresses."),
            new CommandPreset(
                name: "Last 100 journal lines",
                category: "Logs",
                commandTemplate: "journalctl -n 100 --no-pager",
                description: "Tail the system journal (requires systemd)."),
            new CommandPreset(
                name: "Listening ports",
                category: "Network",
                commandTemplate: "ss -tulnp",
                description: "Show processes listening on TCP/UDP ports.")
        };

        SelectedPreset = Presets.FirstOrDefault();

        RunSelectedCommandCommand = new AsyncRelayCommand(RunSelectedAsync, CanRun);
    }

    public ObservableCollection<CommandPreset> Presets { get; }

    public CommandPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetProperty(ref _selectedPreset, value))
            {
                OnPropertyChanged(nameof(CommandPreview));
                Status = value is null
                    ? "Select a preset to see details."
                    : $"Ready to run: {value.Name}";
            }
        }
    }

    public string AdditionalArguments
    {
        get => _additionalArguments;
        set
        {
            if (SetProperty(ref _additionalArguments, value))
            {
                OnPropertyChanged(nameof(CommandPreview));
            }
        }
    }

    public string CommandPreview
    {
        get
        {
            if (SelectedPreset is null)
                return string.Empty;

            var cmd = SelectedPreset.CommandTemplate;
            if (!string.IsNullOrWhiteSpace(AdditionalArguments))
            {
                cmd += " " + AdditionalArguments.Trim();
            }

            return cmd;
        }
    }

    public string Output
    {
        get => _output;
        set => SetProperty(ref _output, value);
    }

    public string Error
    {
        get => _error;
        set => SetProperty(ref _error, value);
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
                RunSelectedCommandCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand RunSelectedCommandCommand { get; }

    private bool CanRun() => !IsBusy && SelectedPreset is not null;

    private async Task RunSelectedAsync()
    {
        if (SelectedPreset is null)
            return;

        IsBusy = true;
        Status = "Running...";
        Output = string.Empty;
        Error = string.Empty;

        var command = CommandPreview;

        var result = await _shellService.RunCommandAsync(command);

        Output = string.IsNullOrWhiteSpace(result.StdOutput)
            ? "(no standard output)"
            : result.StdOutput;

        Error = string.IsNullOrWhiteSpace(result.StdError)
            ? "(no error output)"
            : result.StdError;

        Status = result.IsSuccess
            ? $"Completed in {result.Duration.TotalSeconds:F2}s (exit code {result.ExitCode})."
            : $"Failed in {result.Duration.TotalSeconds:F2}s (exit code {result.ExitCode}).";

        IsBusy = false;
    }
}