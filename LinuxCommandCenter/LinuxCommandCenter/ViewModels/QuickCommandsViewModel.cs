using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Models;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels
{
    public class QuickCommandsViewModel : ViewModelBase
    {
        private readonly ShellService _shellService = new();
        private string _customCommand = string.Empty;
        private bool _useSudo;
        private CommandResult? _lastResult;
        private string _searchTerm = string.Empty;
        private CommandPreset? _selectedPreset;

        public ObservableCollection<CommandPreset> CommandPresets { get; } = new();
        public ObservableCollection<CommandPreset> FilteredPresets { get; } = new();
        public ObservableCollection<CommandResult> CommandHistory { get; } = new();

        public AsyncRelayCommand ExecuteCustomCommandCommand { get; }
        public AsyncRelayCommand ExecutePresetCommand { get; }
        public AsyncRelayCommand ClearHistoryCommand { get; }

        public string CustomCommand
        {
            get => _customCommand;
            set => SetField(ref _customCommand, value);
        }

        public bool UseSudo
        {
            get => _useSudo;
            set => SetField(ref _useSudo, value);
        }

        public CommandResult? LastResult
        {
            get => _lastResult;
            set => SetField(ref _lastResult, value);
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                SetField(ref _searchTerm, value);
                FilterPresets();
            }
        }

        public CommandPreset? SelectedPreset
        {
            get => _selectedPreset;
            set => SetField(ref _selectedPreset, value);
        }

        public QuickCommandsViewModel()
        {
            InitializePresets();
            FilterPresets();

            ExecuteCustomCommandCommand = new AsyncRelayCommand(ExecuteCustomCommandAsync);
            ExecutePresetCommand = new AsyncRelayCommand(ExecutePresetCommandAsync);
            ClearHistoryCommand = new AsyncRelayCommand(ClearHistoryAsync);
        }

        private void InitializePresets()
        {
            // System Information
            CommandPresets.Add(new CommandPreset
            {
                Name = "System Info",
                Command = "uname -a && lsb_release -a 2>/dev/null || cat /etc/os-release",
                Description = "Display system and OS information",
                Category = "System",
                RequiresSudo = false,
                IsFavorite = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "Disk Usage",
                Command = "df -h",
                Description = "Show disk space usage",
                Category = "System",
                RequiresSudo = false,
                IsFavorite = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "Memory Info",
                Command = "free -h",
                Description = "Display memory usage",
                Category = "System",
                RequiresSudo = false,
                IsFavorite = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "CPU Info",
                Command = "lscpu",
                Description = "Show CPU information",
                Category = "System",
                RequiresSudo = false
            });

            // Process Management
            CommandPresets.Add(new CommandPreset
            {
                Name = "Top Processes",
                Command = "ps aux --sort=-%cpu | head -10",
                Description = "Show top 10 CPU consuming processes",
                Category = "Processes",
                RequiresSudo = false,
                IsFavorite = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "Kill Process",
                Command = "kill -9 {PID}",
                Description = "Force kill a process",
                Category = "Processes",
                RequiresSudo = false
            });

            // Network
            CommandPresets.Add(new CommandPreset
            {
                Name = "Network Info",
                Command = "ip addr show",
                Description = "Display network interfaces and IP addresses",
                Category = "Network",
                RequiresSudo = false,
                IsFavorite = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "Open Ports",
                Command = "ss -tulpn",
                Description = "Show listening ports and processes",
                Category = "Network",
                RequiresSudo = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "Ping Test",
                Command = "ping -c 4 google.com",
                Description = "Test network connectivity",
                Category = "Network",
                RequiresSudo = false
            });

            // File Operations
            CommandPresets.Add(new CommandPreset
            {
                Name = "List Files",
                Command = "ls -la",
                Description = "List files with details",
                Category = "Files",
                RequiresSudo = false,
                IsFavorite = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "Find Files",
                Command = "find /path -name '*.txt' -type f",
                Description = "Find files by pattern",
                Category = "Files",
                RequiresSudo = false
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "File Permissions",
                Command = "stat {filename}",
                Description = "Show file permissions and attributes",
                Category = "Files",
                RequiresSudo = false
            });

            // Services
            CommandPresets.Add(new CommandPreset
            {
                Name = "Service Status",
                Command = "systemctl status {service}",
                Description = "Check service status",
                Category = "Services",
                RequiresSudo = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "Restart Service",
                Command = "systemctl restart {service}",
                Description = "Restart a system service",
                Category = "Services",
                RequiresSudo = true
            });

            // Users
            CommandPresets.Add(new CommandPreset
            {
                Name = "Logged Users",
                Command = "who",
                Description = "Show logged in users",
                Category = "Users",
                RequiresSudo = false
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "User Info",
                Command = "id",
                Description = "Display user and group information",
                Category = "Users",
                RequiresSudo = false
            });
        }

        private void FilterPresets()
        {
            FilteredPresets.Clear();
            var query = string.IsNullOrWhiteSpace(SearchTerm)
                ? CommandPresets
                : CommandPresets.Where(p =>
                    p.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

            foreach (var preset in query.OrderBy(p => p.Category).ThenBy(p => p.Name))
            {
                FilteredPresets.Add(preset);
            }
        }

        private async Task ExecuteCustomCommandAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomCommand))
                return;

            var result = await _shellService.ExecuteCommandAsync(CustomCommand, UseSudo);
            LastResult = result;
            CommandHistory.Insert(0, result);

            if (CommandHistory.Count > 50)
                CommandHistory.RemoveAt(CommandHistory.Count - 1);
        }

        private async Task ExecutePresetCommandAsync()
        {
            if (SelectedPreset == null)
                return;

            var command = SelectedPreset.Command;
            CustomCommand = command;
            UseSudo = SelectedPreset.RequiresSudo;

            await ExecuteCustomCommandAsync();
        }

        private Task ClearHistoryAsync()
        {
            CommandHistory.Clear();
            return Task.CompletedTask;
        }
    }
}