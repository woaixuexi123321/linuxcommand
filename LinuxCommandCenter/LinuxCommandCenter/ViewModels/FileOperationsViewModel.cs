using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Models;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels
{
    public class FileOperationsViewModel : ViewModelBase
    {
        private readonly ShellService _shellService = new();
        private string _currentDirectory = "~";
        private string _selectedFile = string.Empty;
        private string _destinationPath = string.Empty;
        private string _filePermissions = "644";
        private string _fileOwner = string.Empty;
        private string _fileGroup = string.Empty;
        private FileOperationType _selectedOperation = FileOperationType.Copy;
        private bool _recursiveOperation;

        public ObservableCollection<string> DirectoryContents { get; } = new();
        public ObservableCollection<FileOperationType> OperationTypes { get; } = new();

        public AsyncRelayCommand NavigateToHomeCommand { get; }
        public AsyncRelayCommand NavigateUpCommand { get; }
        public AsyncRelayCommand RefreshDirectoryCommand { get; }
        public AsyncRelayCommand ExecuteFileOperationCommand { get; }
        public AsyncRelayCommand CreateDirectoryCommand { get; }
        public AsyncRelayCommand CreateFileCommand { get; }
        public AsyncRelayCommand ChangePermissionsCommand { get; }

        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                if (SetField(ref _currentDirectory, value))
                {
                    // 自动刷新目录内容
                    _ = RefreshDirectoryAsync();
                }
            }
        }

        public string SelectedFile
        {
            get => _selectedFile;
            set => SetField(ref _selectedFile, value);
        }

        public string DestinationPath
        {
            get => _destinationPath;
            set => SetField(ref _destinationPath, value);
        }

        public string FilePermissions
        {
            get => _filePermissions;
            set => SetField(ref _filePermissions, value);
        }

        public string FileOwner
        {
            get => _fileOwner;
            set => SetField(ref _fileOwner, value);
        }

        public string FileGroup
        {
            get => _fileGroup;
            set => SetField(ref _fileGroup, value);
        }

        public FileOperationType SelectedOperation
        {
            get => _selectedOperation;
            set => SetField(ref _selectedOperation, value);
        }

        public bool RecursiveOperation
        {
            get => _recursiveOperation;
            set => SetField(ref _recursiveOperation, value);
        }

        public FileOperationsViewModel()
        {
            InitializeOperationTypes();

            NavigateToHomeCommand = new AsyncRelayCommand(NavigateToHomeAsync);
            NavigateUpCommand = new AsyncRelayCommand(NavigateUpAsync);
            RefreshDirectoryCommand = new AsyncRelayCommand(RefreshDirectoryAsync);
            ExecuteFileOperationCommand = new AsyncRelayCommand(ExecuteFileOperationAsync);
            CreateDirectoryCommand = new AsyncRelayCommand(CreateDirectoryAsync);
            CreateFileCommand = new AsyncRelayCommand(CreateFileAsync);
            ChangePermissionsCommand = new AsyncRelayCommand(ChangePermissionsAsync);

            // 初始刷新目录（延迟执行以避免UI阻塞）
            Task.Delay(100).ContinueWith(_ => RefreshDirectoryAsync());
        }

        private void InitializeOperationTypes()
        {
            foreach (FileOperationType type in Enum.GetValues(typeof(FileOperationType)))
            {
                OperationTypes.Add(type);
            }
        }

        private async Task NavigateToHomeAsync()
        {
            CurrentDirectory = "~";
        }

        private async Task NavigateUpAsync()
        {
            var currentPath = ResolvePath(CurrentDirectory);

            if (currentPath != "/")
            {
                var parentDir = Directory.GetParent(currentPath);
                if (parentDir != null)
                {
                    CurrentDirectory = parentDir.FullName;
                }
            }
        }

        private async Task RefreshDirectoryAsync()
        {
            DirectoryContents.Clear();

            try
            {
                var resolvedPath = ResolvePath(CurrentDirectory);

                // 执行列出文件的命令
                var result = await _shellService.ExecuteCommandAsync($"ls -la \"{resolvedPath}\"");

                if (result.IsSuccess)
                {
                    var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        // 跳过"total xxx"行
                        if (!line.StartsWith("total"))
                        {
                            DirectoryContents.Add(line);
                        }
                    }
                }
                else
                {
                    // 显示错误信息
                    DirectoryContents.Add($"Error: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                DirectoryContents.Add($"Exception: {ex.Message}");
            }
        }

        private string ResolvePath(string path)
        {
            // 将~转换为用户主目录
            if (path.StartsWith("~"))
            {
                var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return homePath + path.Substring(1);
            }
            return path;
        }

        private async Task ExecuteFileOperationAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedFile))
            {
                DirectoryContents.Add("Error: No file selected");
                return;
            }

            var sourcePath = ResolvePath(SelectedFile);
            var destPath = ResolvePath(DestinationPath);

            string command = SelectedOperation switch
            {
                FileOperationType.Copy => $"cp {(RecursiveOperation ? "-r " : "")}\"{sourcePath}\" \"{destPath}\"",
                FileOperationType.Move => $"mv \"{sourcePath}\" \"{destPath}\"",
                FileOperationType.Delete => $"rm {(RecursiveOperation ? "-r " : "")}-f \"{sourcePath}\"",
                FileOperationType.Rename => $"mv \"{sourcePath}\" \"{destPath}\"",
                FileOperationType.Compress => $"tar -czf \"{destPath}.tar.gz\" \"{sourcePath}\"",
                FileOperationType.Extract => $"tar -xzf \"{sourcePath}\" -C \"{destPath}\"",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(command))
            {
                var result = await _shellService.ExecuteCommandAsync(command);
                if (result.IsSuccess)
                {
                    await RefreshDirectoryAsync();
                }
                else
                {
                    DirectoryContents.Add($"Operation failed: {result.Error}");
                }
            }
        }

        private async Task CreateDirectoryAsync()
        {
            if (!string.IsNullOrWhiteSpace(DestinationPath))
            {
                var resolvedPath = ResolvePath(DestinationPath);
                var result = await _shellService.ExecuteCommandAsync($"mkdir -p \"{resolvedPath}\"");
                if (result.IsSuccess)
                {
                    await RefreshDirectoryAsync();
                }
            }
        }

        private async Task CreateFileAsync()
        {
            if (!string.IsNullOrWhiteSpace(DestinationPath))
            {
                var resolvedPath = ResolvePath(DestinationPath);
                var result = await _shellService.ExecuteCommandAsync($"touch \"{resolvedPath}\"");
                if (result.IsSuccess)
                {
                    await RefreshDirectoryAsync();
                }
            }
        }

        private async Task ChangePermissionsAsync()
        {
            if (!string.IsNullOrWhiteSpace(SelectedFile))
            {
                var resolvedPath = ResolvePath(SelectedFile);
                var result = await _shellService.ExecuteCommandAsync($"chmod {FilePermissions} \"{resolvedPath}\"");
                if (result.IsSuccess)
                {
                    await RefreshDirectoryAsync();
                }
            }
        }
    }
}