using System.Collections.ObjectModel;
using LinuxCommandCenter.Services;

namespace LinuxCommandCenter.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private NavigationItemViewModel? _selectedNavigationItem;

    public MainWindowViewModel()
    {
        Title = "Linux Command Center";

        var shellService = new ShellService();

        var quickCommandsVm = new QuickCommandsViewModel(shellService);
        var fileOperationsVm = new FileOperationsViewModel(shellService);
        var systemToolsVm = new SystemToolsViewModel(shellService);
        var logViewerVm = new LogViewerViewModel(shellService);

        NavigationItems = new ObservableCollection<NavigationItemViewModel>
        {
            new NavigationItemViewModel("Quick commands", "Run common tasks with one click", quickCommandsVm),
            new NavigationItemViewModel("File operations", "Copy, move, delete and archive files", fileOperationsVm),
            new NavigationItemViewModel("System tools", "Inspect system health and resources", systemToolsVm),
            new NavigationItemViewModel("Command log", "Review history and outputs", logViewerVm)
        };

        SelectedNavigationItem = NavigationItems[0];
    }

    public string Title { get; }

    public string AppVersion => "v1.0.0";

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public NavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                foreach (var item in NavigationItems)
                {
                    item.IsSelected = ReferenceEquals(item, value);
                }

                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }
    }

    public ViewModelBase? CurrentViewModel => SelectedNavigationItem?.ViewModel;
}