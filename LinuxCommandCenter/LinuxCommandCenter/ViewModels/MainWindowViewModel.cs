using LinuxCommandCenter.Commands;
using LinuxCommandCenter.Services;
using LinuxCommandCenter.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LinuxCommandCenter.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ShellService _shellService = new();
        private ViewModelBase? _currentViewModel;
        private string _statusMessage = "Ready";
        private bool _isConnected;
        private string _connectionStatus = "Disconnected";

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = new();
        public AsyncRelayCommand TestConnectionCommand { get; }
        public AsyncRelayCommand RefreshSystemInfoCommand { get; }

        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetField(ref _currentViewModel, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetField(ref _isConnected, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetField(ref _connectionStatus, value);
        }

        public MainWindowViewModel()
        {
            InitializeNavigation();
            TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
            RefreshSystemInfoCommand = new AsyncRelayCommand(RefreshSystemInfoAsync);
        }

        private void InitializeNavigation()
        {
            NavigationItems.Add(new NavigationItemViewModel
            {
                Name = "Dashboard",
                Icon = "M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z",
                ViewModelType = typeof(QuickCommandsViewModel),
                OnSelectedCommand = new RelayCommand(() => ShowViewModel<QuickCommandsViewModel>())
            });

            NavigationItems.Add(new NavigationItemViewModel
            {
                Name = "File Manager",
                Icon = "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z",
                ViewModelType = typeof(FileOperationsViewModel),
                OnSelectedCommand = new RelayCommand(() => ShowViewModel<FileOperationsViewModel>())
            });

            NavigationItems.Add(new NavigationItemViewModel
            {
                Name = "System Tools",
                Icon = "M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z",
                ViewModelType = typeof(SystemToolsViewModel),
                OnSelectedCommand = new RelayCommand(() => ShowViewModel<SystemToolsViewModel>())
            });

            NavigationItems.Add(new NavigationItemViewModel
            {
                Name = "Log Viewer",
                Icon = "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z",
                ViewModelType = typeof(LogViewerViewModel),
                OnSelectedCommand = new RelayCommand(() => ShowViewModel<LogViewerViewModel>())
            });

            // Select first item by default
            if (NavigationItems.Count > 0)
            {
                NavigationItems[0].IsSelected = true;
                NavigationItems[0].OnSelectedCommand?.Execute(null);
            }
        }
        private void ShowViewModel<T>() where T : ViewModelBase, new()
        {
            CurrentViewModel = new T();
        }

        private async Task TestConnectionAsync()
        {
            StatusMessage = "Testing connection...";
            var result = await _shellService.TestConnectionAsync();

            IsConnected = result.IsSuccess;
            ConnectionStatus = result.IsSuccess ? "Connected" : "Disconnected";
            StatusMessage = result.IsSuccess
                ? $"Connected as {result.Output.Split('\n')[1].Trim()}"
                : $"Connection failed: {result.Error}";
        }

        private async Task RefreshSystemInfoAsync()
        {
            StatusMessage = "Refreshing system info...";
            var result = await _shellService.GetSystemInfoAsync();

            if (result.IsSuccess)
            {
                StatusMessage = "System info refreshed";
            }
            else
            {
                StatusMessage = $"Failed to get system info: {result.Error}";
            }
        }
    }
}