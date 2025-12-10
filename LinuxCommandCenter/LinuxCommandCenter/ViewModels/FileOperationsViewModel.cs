using System;
using System.Collections.ObjectModel;
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
                SetField(ref _currentDirectory, value);
                RefreshDirectoryAsync().ConfigureAwait(false);
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

            RefreshDirectoryAsync().ConfigureAwait(false);
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
            if (CurrentDirectory != "/" && !string.IsNullOrWhiteSpace(CurrentDirectory))
            {
                var dir = System.IO.Path.GetDirectoryName(CurrentDirectory);
                if (!string.IsNullOrEmpty(dir))
                {
                    CurrentDirectory = dir;
                }
            }
        }

        private async Task RefreshDirectoryAsync()
        {
            DirectoryContents.Clear();

            var result = await _shellService.ExecuteCommandAsync($"ls -la \"{CurrentDirectory}\"");

            if (result.IsSuccess)
            {
                var lines = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (!line.StartsWith("total"))
                    {
                        DirectoryContents.Add(line);
                    }
                }
            }
        }

        private async Task ExecuteFileOperationAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedFile))
                return;

            string command = SelectedOperation switch
            {
                FileOperationType.Copy => $"cp {(RecursiveOperation ? "-r " : "")}\"{SelectedFile}\" \"{DestinationPath}\"",
                FileOperationType.Move => $"mv \"{SelectedFile}\" \"{DestinationPath}\"",
                FileOperationType.Delete => $"rm {(RecursiveOperation ? "-r " : "")}-f \"{SelectedFile}\"",
                FileOperationType.Rename => $"mv \"{SelectedFile}\" \"{DestinationPath}\"",
                FileOperationType.Compress => $"tar -czf \"{DestinationPath}.tar.gz\" \"{SelectedFile}\"",
                FileOperationType.Extract => $"tar -xzf \"{SelectedFile}\" -C \"{DestinationPath}\"",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(command))
            {
                var result = await _shellService.ExecuteCommandAsync(command);
                if (result.IsSuccess)
                {
                    await RefreshDirectoryAsync();
                }
            }
        }

        private async Task CreateDirectoryAsync()
        {
            if (!string.IsNullOrWhiteSpace(DestinationPath))
            {
                var result = await _shellService.ExecuteCommandAsync($"mkdir -p \"{DestinationPath}\"");
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
                var result = await _shellService.ExecuteCommandAsync($"touch \"{DestinationPath}\"");
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
                var result = await _shellService.ExecuteCommandAsync($"chmod {FilePermissions} \"{SelectedFile}\"");
                if (result.IsSuccess)
                {
                    await RefreshDirectoryAsync();
                }
            }
        }
    }
}