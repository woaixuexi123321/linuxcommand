using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Models;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels;

public class LogViewerViewModel : ViewModelBase
{
    private readonly ShellService _shellService;

    private string _searchText = string.Empty;
    private CommandResult? _selectedResult;

    public LogViewerViewModel(ShellService shellService)
    {
        _shellService = shellService;

        AllResults = new ObservableCollection<CommandResult>();
        FilteredResults = new ObservableCollection<CommandResult>();

        ClearCommand = new RelayCommand(Clear);

        _shellService.CommandExecuted += OnCommandExecuted;
    }

    public ObservableCollection<CommandResult> AllResults { get; }
    public ObservableCollection<CommandResult> FilteredResults { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                UpdateFilter();
            }
        }
    }

    public CommandResult? SelectedResult
    {
        get => _selectedResult;
        set => SetProperty(ref _selectedResult, value);
    }

    public RelayCommand ClearCommand { get; }

    private void OnCommandExecuted(object? sender, CommandResult result)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            AddResultInternal(result);
        }
        else
        {
            Dispatcher.UIThread.Post(() => AddResultInternal(result));
        }
    }

    private void AddResultInternal(CommandResult result)
    {
        AllResults.Insert(0, result);
        UpdateFilter();
    }

    private void UpdateFilter()
    {
        FilteredResults.Clear();

        var text = SearchText?.Trim();
        var query = AllResults.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(text))
        {
            text = text.ToLowerInvariant();
            query = query.Where(r =>
                (r.CommandText?.ToLowerInvariant().Contains(text) ?? false) ||
                (r.StdOutput?.ToLowerInvariant().Contains(text) ?? false) ||
                (r.StdError?.ToLowerInvariant().Contains(text) ?? false));
        }

        foreach (var result in query)
        {
            FilteredResults.Add(result);
        }

        if (FilteredResults.Count > 0 && SelectedResult is null)
        {
            SelectedResult = FilteredResults[0];
        }
    }

    private void Clear()
    {
        AllResults.Clear();
        FilteredResults.Clear();
        SelectedResult = null;
    }
}