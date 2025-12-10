using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LinuxCommandCenter.Views
{
    public partial class FileOperationsView : UserControl
    {
        public FileOperationsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}