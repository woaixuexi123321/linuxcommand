using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace LinuxCommandCenter.ViewModels
{
    public class NavigationItemViewModel : ViewModelBase
    {
        private string _name = string.Empty;
        private string _icon = string.Empty;
        private bool _isSelected;
        private Type? _viewModelType;

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Icon
        {
            get => _icon;
            set => SetField(ref _icon, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        public Type? ViewModelType
        {
            get => _viewModelType;
            set => SetField(ref _viewModelType, value);
        }

        public ICommand? OnSelectedCommand { get; set; }
    }
}