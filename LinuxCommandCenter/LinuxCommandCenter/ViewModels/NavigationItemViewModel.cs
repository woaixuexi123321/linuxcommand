namespace LinuxCommandCenter.ViewModels;

public class NavigationItemViewModel : ViewModelBase
{
    private bool _isSelected;

    public NavigationItemViewModel(string title, string subtitle, ViewModelBase viewModel)
    {
        Title = title;
        Subtitle = subtitle;
        ViewModel = viewModel;
    }

    public string Title { get; }
    public string Subtitle { get; }
    public ViewModelBase ViewModel { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}