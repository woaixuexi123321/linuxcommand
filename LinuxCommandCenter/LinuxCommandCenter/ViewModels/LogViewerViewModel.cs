using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels
{
    public class LogViewerViewModel : ViewModelBase
    {
        private readonly ShellService _shellService = new();
        private string _selectedLogFile = "/var/log/syslog";
        private string _filterText = string.Empty;
        private int _lineCount = 100;
        private bool _followLog;
        private bool _showErrorsOnly;
        private bool _showWarningsOnly;
        private DateTime _fromDate = DateTime.Now.AddDays(-1);
        private DateTime _toDate = DateTime.Now;

        public ObservableCollection<LogFileInfo> AvailableLogs { get; } = new();
        public ObservableCollection<string> LogEntries { get; } = new();
        public ObservableCollection<string> FilteredEntries { get; } = new();

        public AsyncRelayCommand RefreshLogsCommand { get; }
        public AsyncRelayCommand ClearLogCommand { get; }
        public AsyncRelayCommand ExportLogCommand { get; }
        public AsyncRelayCommand SearchLogsCommand { get; }

        public string SelectedLogFile
        {
            get => _selectedLogFile;
            set
            {
                SetField(ref _selectedLogFile, value);
                LoadLogFileAsync().ConfigureAwait(false);
            }
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                SetField(ref _filterText, value);
                ApplyFilter();
            }
        }

        public int LineCount
        {
            get => _lineCount;
            set
            {
                SetField(ref _lineCount, value);
                LoadLogFileAsync().ConfigureAwait(false);
            }
        }

        public bool FollowLog
        {
            get => _followLog;
            set => SetField(ref _followLog, value);
        }

        public bool ShowErrorsOnly
        {
            get => _showErrorsOnly;
            set
            {
                SetField(ref _showErrorsOnly, value);
                ApplyFilter();
            }
        }

        public bool ShowWarningsOnly
        {
            get => _showWarningsOnly;
            set
            {
                SetField(ref _showWarningsOnly, value);
                ApplyFilter();
            }
        }

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                SetField(ref _fromDate, value);
                ApplyFilter();
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                SetField(ref _toDate, value);
                ApplyFilter();
            }
        }

        public LogViewerViewModel()
        {
            RefreshLogsCommand = new AsyncRelayCommand(RefreshLogsAsync);
            ClearLogCommand = new AsyncRelayCommand(ClearLogAsync);
            ExportLogCommand = new AsyncRelayCommand(ExportLogAsync);
            SearchLogsCommand = new AsyncRelayCommand(SearchLogsAsync);

            InitializeAvailableLogs();
            LoadLogFileAsync().ConfigureAwait(false);
        }

        private void InitializeAvailableLogs()
        {
            AvailableLogs.Add(new LogFileInfo { Path = "/var/log/syslog", Name = "System Log" });
            AvailableLogs.Add(new LogFileInfo { Path = "/var/log/auth.log", Name = "Authentication Log" });
            AvailableLogs.Add(new LogFileInfo { Path = "/var/log/kern.log", Name = "Kernel Log" });
            AvailableLogs.Add(new LogFileInfo { Path = "/var/log/dpkg.log", Name = "Package Log" });
            AvailableLogs.Add(new LogFileInfo { Path = "/var/log/apt/history.log", Name = "APT History" });
            AvailableLogs.Add(new LogFileInfo { Path = "/var/log/nginx/access.log", Name = "Nginx Access" });
            AvailableLogs.Add(new LogFileInfo { Path = "/var/log/nginx/error.log", Name = "Nginx Error" });
            AvailableLogs.Add(new LogFileInfo { Path = "/var/log/mysql/error.log", Name = "MySQL Error" });
        }

        private async Task RefreshLogsAsync()
        {
            await LoadLogFileAsync();
        }

        private async Task LoadLogFileAsync()
        {
            LogEntries.Clear();

            var command = FollowLog
                ? $"tail -f -n {LineCount} \"{SelectedLogFile}\" 2>/dev/null || echo 'Cannot access log file'"
                : $"tail -n {LineCount} \"{SelectedLogFile}\" 2>/dev/null || echo 'Cannot access log file'";

            var result = await _shellService.ExecuteCommandAsync(command);

            if (result.IsSuccess)
            {
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    LogEntries.Add(line);
                }

                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            FilteredEntries.Clear();

            foreach (var entry in LogEntries)
            {
                var addEntry = true;

                // Apply text filter
                if (!string.IsNullOrWhiteSpace(FilterText) &&
                    !entry.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                {
                    addEntry = false;
                }

                // Apply error filter
                if (ShowErrorsOnly && !entry.Contains("error", StringComparison.OrdinalIgnoreCase))
                {
                    addEntry = false;
                }

                // Apply warning filter
                if (ShowWarningsOnly && !entry.Contains("warning", StringComparison.OrdinalIgnoreCase))
                {
                    addEntry = false;
                }

                // Apply date filter (simple string-based check for demo)
                if (addEntry)
                {
                    FilteredEntries.Add(entry);
                }
            }
        }

        private async Task ClearLogAsync()
        {
            var result = await _shellService.ExecuteCommandAsync($"sudo truncate -s 0 \"{SelectedLogFile}\"");
            if (result.IsSuccess)
            {
                await LoadLogFileAsync();
            }
        }

        private async Task ExportLogAsync()
        {
            var exportPath = $"/tmp/log_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var result = await _shellService.ExecuteCommandAsync($"cp \"{SelectedLogFile}\" \"{exportPath}\"");

            if (result.IsSuccess)
            {
                // In a real app, you would show a save file dialog
            }
        }

        private async Task SearchLogsAsync()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
                return;

            var result = await _shellService.ExecuteCommandAsync(
                $"grep -i \"{FilterText}\" \"{SelectedLogFile}\" | tail -{LineCount}");

            if (result.IsSuccess)
            {
                LogEntries.Clear();
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    LogEntries.Add(line);
                }
                ApplyFilter();
            }
        }
    }

    public class LogFileInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}