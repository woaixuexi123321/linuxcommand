using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Models;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels
{
    public class SystemToolsViewModel : ViewModelBase
    {
        private readonly ShellService _shellService = new();
        private string _serviceName = string.Empty;
        private string _packageName = string.Empty;
        private string _processName = string.Empty;
        private int _processId;
        private string _logFile = "/var/log/syslog";
        private bool _autoRefresh;
        private System.DateTime _lastRefresh = System.DateTime.MinValue;

        public ObservableCollection<SystemProcess> Processes { get; } = new();
        public ObservableCollection<SystemService> Services { get; } = new();
        public ObservableCollection<string> LogLines { get; } = new();
        public ObservableCollection<SystemPackage> Packages { get; } = new();

        public AsyncRelayCommand RefreshProcessesCommand { get; }
        public AsyncRelayCommand RefreshServicesCommand { get; }
        public AsyncRelayCommand RefreshLogsCommand { get; }
        public AsyncRelayCommand KillProcessCommand { get; }
        public AsyncRelayCommand StartServiceCommand { get; }
        public AsyncRelayCommand StopServiceCommand { get; }
        public AsyncRelayCommand RestartServiceCommand { get; }
        public AsyncRelayCommand InstallPackageCommand { get; }
        public AsyncRelayCommand RemovePackageCommand { get; }
        public AsyncRelayCommand UpdateSystemCommand { get; }

        public string ServiceName
        {
            get => _serviceName;
            set => SetField(ref _serviceName, value);
        }

        public string PackageName
        {
            get => _packageName;
            set => SetField(ref _packageName, value);
        }

        public string ProcessName
        {
            get => _processName;
            set => SetField(ref _processName, value);
        }

        public int ProcessId
        {
            get => _processId;
            set => SetField(ref _processId, value);
        }

        public string LogFile
        {
            get => _logFile;
            set
            {
                SetField(ref _logFile, value);
                RefreshLogsAsync().ConfigureAwait(false);
            }
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                SetField(ref _autoRefresh, value);
                if (value)
                {
                    StartAutoRefresh();
                }
            }
        }

        public SystemToolsViewModel()
        {
            RefreshProcessesCommand = new AsyncRelayCommand(RefreshProcessesAsync);
            RefreshServicesCommand = new AsyncRelayCommand(RefreshServicesAsync);
            RefreshLogsCommand = new AsyncRelayCommand(RefreshLogsAsync);
            KillProcessCommand = new AsyncRelayCommand(KillProcessAsync);
            StartServiceCommand = new AsyncRelayCommand(StartServiceAsync);
            StopServiceCommand = new AsyncRelayCommand(StopServiceAsync);
            RestartServiceCommand = new AsyncRelayCommand(RestartServiceAsync);
            InstallPackageCommand = new AsyncRelayCommand(InstallPackageAsync);
            RemovePackageCommand = new AsyncRelayCommand(RemovePackageAsync);
            UpdateSystemCommand = new AsyncRelayCommand(UpdateSystemAsync);

            // Initial refresh
            RefreshProcessesAsync().ConfigureAwait(false);
            RefreshServicesAsync().ConfigureAwait(false);
            RefreshLogsAsync().ConfigureAwait(false);
        }

        private async Task RefreshProcessesAsync()
        {
            Processes.Clear();
            var result = await _shellService.GetProcessListAsync();

            if (result.IsSuccess)
            {
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < Math.Min(lines.Length, 21); i++) // Skip header
                {
                    var parts = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 11)
                    {
                        Processes.Add(new SystemProcess
                        {
                            User = parts[0],
                            Pid = int.Parse(parts[1]),
                            CpuUsage = parts[2],
                            MemoryUsage = parts[3],
                            Command = string.Join(" ", parts, 10, parts.Length - 10)
                        });
                    }
                }
                _lastRefresh = System.DateTime.Now;
            }
        }

        private async Task RefreshServicesAsync()
        {
            Services.Clear();
            var result = await _shellService.ExecuteCommandAsync("systemctl list-units --type=service --all --no-pager");

            if (result.IsSuccess)
            {
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Contains(".service"))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            Services.Add(new SystemService
                            {
                                Name = parts[0],
                                LoadState = parts[1],
                                ActiveState = parts[2],
                                SubState = parts[3],
                                Description = parts.Length > 4 ? string.Join(" ", parts, 4, parts.Length - 4) : ""
                            });
                        }
                    }
                }
            }
        }

        private async Task RefreshLogsAsync()
        {
            LogLines.Clear();
            var result = await _shellService.ExecuteCommandAsync($"tail -50 {LogFile} 2>/dev/null || echo 'Log file not accessible'");

            if (result.IsSuccess)
            {
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    LogLines.Add(line);
                }
            }
        }

        private async Task KillProcessAsync()
        {
            if (ProcessId > 0)
            {
                var result = await _shellService.ExecuteCommandAsync($"kill -9 {ProcessId}");
                if (result.IsSuccess)
                {
                    await RefreshProcessesAsync();
                }
            }
        }

        private async Task StartServiceAsync()
        {
            if (!string.IsNullOrWhiteSpace(ServiceName))
            {
                var result = await _shellService.ExecuteCommandAsync($"systemctl start {ServiceName}");
                if (result.IsSuccess)
                {
                    await RefreshServicesAsync();
                }
            }
        }

        private async Task StopServiceAsync()
        {
            if (!string.IsNullOrWhiteSpace(ServiceName))
            {
                var result = await _shellService.ExecuteCommandAsync($"systemctl stop {ServiceName}");
                if (result.IsSuccess)
                {
                    await RefreshServicesAsync();
                }
            }
        }

        private async Task RestartServiceAsync()
        {
            if (!string.IsNullOrWhiteSpace(ServiceName))
            {
                var result = await _shellService.ExecuteCommandAsync($"systemctl restart {ServiceName}");
                if (result.IsSuccess)
                {
                    await RefreshServicesAsync();
                }
            }
        }

        private async Task InstallPackageAsync()
        {
            if (!string.IsNullOrWhiteSpace(PackageName))
            {
                var result = await _shellService.ExecuteCommandAsync($"sudo apt-get install -y {PackageName}");
                if (result.IsSuccess)
                {
                    // Refresh package list
                }
            }
        }

        private async Task RemovePackageAsync()
        {
            if (!string.IsNullOrWhiteSpace(PackageName))
            {
                var result = await _shellService.ExecuteCommandAsync($"sudo apt-get remove -y {PackageName}");
                if (result.IsSuccess)
                {
                    // Refresh package list
                }
            }
        }

        private async Task UpdateSystemAsync()
        {
            var result = await _shellService.ExecuteCommandAsync("sudo apt-get update && sudo apt-get upgrade -y");
        }

        private async void StartAutoRefresh()
        {
            while (AutoRefresh)
            {
                await Task.Delay(5000); // Refresh every 5 seconds
                if (AutoRefresh)
                {
                    await RefreshProcessesAsync();
                }
            }
        }
    }

    public class SystemProcess
    {
        public string User { get; set; } = string.Empty;
        public int Pid { get; set; }
        public string CpuUsage { get; set; } = string.Empty;
        public string MemoryUsage { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
    }

    public class SystemService
    {
        public string Name { get; set; } = string.Empty;
        public string LoadState { get; set; } = string.Empty;
        public string ActiveState { get; set; } = string.Empty;
        public string SubState { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SystemPackage
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}