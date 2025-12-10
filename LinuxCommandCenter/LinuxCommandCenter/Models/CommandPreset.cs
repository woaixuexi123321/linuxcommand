using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LinuxCommandCenter.Models
{
    public class CommandPreset : INotifyPropertyChanged
    {
        private string _name;
        private string _command;
        private string _description;
        private string _category;
        private bool _requiresSudo;
        private bool _isFavorite;

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Command
        {
            get => _command;
            set => SetField(ref _command, value);
        }

        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public string Category
        {
            get => _category;
            set => SetField(ref _category, value);
        }

        public bool RequiresSudo
        {
            get => _requiresSudo;
            set => SetField(ref _requiresSudo, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetField(ref _isFavorite, value);
        }

        public List<string> Arguments { get; set; } = new();
        public Dictionary<string, string> Parameters { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}