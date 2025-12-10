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

        // 命令属性 - 注意：ExecutePresetCommand 是 AsyncRelayCommand<object?> 类型
        public AsyncRelayCommand ExecuteCustomCommandCommand { get; }
        public AsyncRelayCommand<object?> ExecutePresetCommand { get; }
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
                FilterPresets(); // 搜索词改变时自动过滤
            }
        }

        public CommandPreset? SelectedPreset
        {
            get => _selectedPreset;
            set => SetField(ref _selectedPreset, value);
        }

        public QuickCommandsViewModel()
        {
            // 初始化步骤必须按顺序执行：
            // 1. 初始化预设命令集合
            InitializePresets();

            // 2. 初始化命令（确保在InitializePresets之后）
            ExecuteCustomCommandCommand = new AsyncRelayCommand(ExecuteCustomCommandAsync);
            ExecutePresetCommand = new AsyncRelayCommand<object?>(ExecutePresetCommandAsync);
            ClearHistoryCommand = new AsyncRelayCommand(ClearHistoryAsync);

            // 3. 最后填充过滤列表（确保在InitializePresets之后）
            FilterPresets();

            // 调试输出：验证初始化结果
            System.Diagnostics.Debug.WriteLine($"[Debug] Initialized: {CommandPresets.Count} presets, {FilteredPresets.Count} filtered");
        }

        private void InitializePresets()
        {
            // 清空现有预设（如果有）
            CommandPresets.Clear();

            // System Information - 系统信息命令
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

            // Process Management - 进程管理命令
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
                Description = "Force kill a process (replace {PID})",
                Category = "Processes",
                RequiresSudo = false
            });

            // Network - 网络命令
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

            // File Operations - 文件操作命令
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
                Description = "Find files by pattern (replace /path)",
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

            // Services - 服务管理命令
            CommandPresets.Add(new CommandPreset
            {
                Name = "Service Status",
                Command = "systemctl status {service}",
                Description = "Check service status (replace {service})",
                Category = "Services",
                RequiresSudo = true
            });

            CommandPresets.Add(new CommandPreset
            {
                Name = "Restart Service",
                Command = "systemctl restart {service}",
                Description = "Restart a system service (replace {service})",
                Category = "Services",
                RequiresSudo = true
            });

            // Users - 用户管理命令
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

            System.Diagnostics.Debug.WriteLine($"[Debug] Initialized {CommandPresets.Count} command presets");
        }

        private void FilterPresets()
        {
            // 清空当前过滤列表
            FilteredPresets.Clear();

            // 决定要显示的集合
            var sourceCollection = string.IsNullOrWhiteSpace(SearchTerm)
                ? CommandPresets // 无搜索词：显示全部
                : CommandPresets.Where(p =>
                    (p.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Description?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Category?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));

            // 按分类和名称排序后添加到过滤列表
            foreach (var preset in sourceCollection
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name))
            {
                FilteredPresets.Add(preset);
            }

            System.Diagnostics.Debug.WriteLine($"[Debug] Filtered: SearchTerm='{SearchTerm}', Displaying {FilteredPresets.Count} of {CommandPresets.Count} presets");
        }

        private async Task ExecuteCustomCommandAsync()
        {
            if (string.IsNullOrWhiteSpace(CustomCommand))
            {
                System.Diagnostics.Debug.WriteLine("[Debug] Custom command is empty, skipping execution");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Debug] Executing custom command: {CustomCommand} (sudo: {UseSudo})");

            var result = await _shellService.ExecuteCommandAsync(CustomCommand, UseSudo);
            LastResult = result;

            // 添加到历史记录（最新在最前面）
            CommandHistory.Insert(0, result);

            // 限制历史记录数量
            if (CommandHistory.Count > 50)
                CommandHistory.RemoveAt(CommandHistory.Count - 1);

            System.Diagnostics.Debug.WriteLine($"[Debug] Command executed. Success: {result.IsSuccess}, Output length: {result.Output.Length}");
        }

        private async Task ExecutePresetCommandAsync(object? parameter)
        {
            System.Diagnostics.Debug.WriteLine($"[Debug] ExecutePresetCommandAsync called with parameter: {parameter?.GetType().Name}");

            CommandPreset? presetToExecute = null;

            // 1. 优先使用参数传递的预设
            if (parameter is CommandPreset presetFromParam)
            {
                presetToExecute = presetFromParam;
                SelectedPreset = presetFromParam; // 同时更新选中状态
                System.Diagnostics.Debug.WriteLine($"[Debug] Using preset from parameter: {presetFromParam.Name}");
            }
            // 2. 如果没有参数但当前有选中的预设，使用它
            else if (SelectedPreset != null)
            {
                presetToExecute = SelectedPreset;
                System.Diagnostics.Debug.WriteLine($"[Debug] Using currently selected preset: {SelectedPreset.Name}");
            }
            // 3. 两者都没有，无法执行
            else
            {
                System.Diagnostics.Debug.WriteLine("[Debug] No preset available to execute");
                return;
            }

            // 执行命令
            CustomCommand = presetToExecute.Command;
            UseSudo = presetToExecute.RequiresSudo;

            System.Diagnostics.Debug.WriteLine($"[Debug] Will execute preset: {presetToExecute.Name}, Command: {presetToExecute.Command}");

            await ExecuteCustomCommandAsync();
        }

        private Task ClearHistoryAsync()
        {
            CommandHistory.Clear();
            System.Diagnostics.Debug.WriteLine("[Debug] Command history cleared");
            return Task.CompletedTask;
        }
    }
}