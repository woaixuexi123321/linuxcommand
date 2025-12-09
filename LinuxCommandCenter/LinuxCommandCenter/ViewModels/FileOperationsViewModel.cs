using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Models;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels;

public class FileOperationsViewModel : ViewModelBase
{
    private readonly ShellService _shellService;

    private FileOperationType _selectedOperation;
    private string _sourcePath = string.Empty;
    private string _destinationPath = string.Empty;
    private bool _includeSubdirectories = true;
    private bool _overwriteExisting = true;
    private bool _deleteConfirmation;
    private string _commandPreview = string.Empty;
    private string _output = string.Empty;
    private string _error = string.Empty;
    private string _status = "Ready";
    private bool _isBusy;

    public FileOperationsViewModel(ShellService shellService)
    {
        _shellService = shellService;

        AvailableOperations = new ObservableCollection<FileOperationType>(
            Enum.GetValues<FileOperationType>());

        SelectedOperation = FileOperationType.Copy;

        ExecuteOperationCommand = new AsyncRelayCommand(ExecuteOperationAsync, CanExecute);
    }

    public ObservableCollection<FileOperationType> AvailableOperations { get; }

    public FileOperationType SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (SetProperty(ref _selectedOperation, value))
            {
                UpdateCommandPreview();
                OnPropertyChanged(nameof(IsDeleteOperation));
            }
        }
    }

    public bool IsDeleteOperation => SelectedOperation == FileOperationType.Delete;

    public string SourcePath
    {
        get => _sourcePath;
        set
        {
            if (SetProperty(ref _sourcePath, value))
            {
                UpdateCommandPreview();
            }
        }
    }

    public string DestinationPath
    {
        get => _destinationPath;
        set
        {
            if (SetProperty(ref _destinationPath, value))
            {
                UpdateCommandPreview();
            }
        }
    }

    public bool IncludeSubdirectories
    {
        get => _includeSubdirectories;
        set
        {
            if (SetProperty(ref _includeSubdirectories, value))
            {
                UpdateCommandPreview();
            }
        }
    }

    public bool OverwriteExisting
    {
        get => _overwriteExisting;
        set
        {
            if (SetProperty(ref _overwriteExisting, value))
            {
                UpdateCommandPreview();
            }
        }
    }

    public bool DeleteConfirmation
    {
        get => _deleteConfirmation;
        set
        {
            if (SetProperty(ref _deleteConfirmation, value))
            {
                ExecuteOperationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string CommandPreview
    {
        get => _commandPreview;
        private set => SetProperty(ref _commandPreview, value);
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
                ExecuteOperationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand ExecuteOperationCommand { get; }

    private void UpdateCommandPreview()
    {
        CommandPreview = BuildCommand();
    }

    private string BuildCommand()
    {
        string Quote(string path) => string.IsNullOrWhiteSpace(path) ? string.Empty : $"\"{path}\"";

        var source = Quote(SourcePath.Trim());
        var destination = Quote(DestinationPath.Trim());

        return SelectedOperation switch
        {
            FileOperationType.Copy => BuildCopyCommand(source, destination),
            FileOperationType.Move => BuildMoveCommand(source, destination),
            FileOperationType.Delete => BuildDeleteCommand(source),
            FileOperationType.Compress => BuildCompressCommand(source, destination),
            _ => string.Empty
        };
    }

    private string BuildCopyCommand(string source, string destination)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            return string.Empty;

        var flags = new List<string>();
        if (IncludeSubdirectories)
        {
            flags.Add("-r");
        }

        if (OverwriteExisting)
        {
            flags.Add("-f");
        }

        var flagPart = flags.Count > 0 ? string.Join(" ", flags) + " " : string.Empty;

        return $"cp {flagPart}{source} {destination}";
    }

    private string BuildMoveCommand(string source, string destination)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            return string.Empty;

        var flags = OverwriteExisting ? "-f " : string.Empty;
        return $"mv {flags}{source} {destination}";
    }

    private string BuildDeleteCommand(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        var flags = IncludeSubdirectories ? "-r" : string.Empty;
        return string.IsNullOrEmpty(flags) ? $"rm {source}" : $"rm {flags} {source}";
    }

    private string BuildCompressCommand(string source, string archivePath)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(archivePath))
            return string.Empty;

        return $"tar -czf {archivePath} {source}";
    }

    private bool CanExecute()
    {
        if (IsBusy)
            return false;

        if (SelectedOperation == FileOperationType.Delete && !DeleteConfirmation)
            return false;

        return !string.IsNullOrWhiteSpace(CommandPreview);
    }

    private async Task ExecuteOperationAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandPreview))
        {
            Status = "Please fill in required fields first.";
            return;
        }

        IsBusy = true;
        Status = "Running...";
        Output = string.Empty;
        Error = string.Empty;

        var result = await _shellService.RunCommandAsync(CommandPreview);

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