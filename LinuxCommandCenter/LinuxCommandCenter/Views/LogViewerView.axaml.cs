using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LinuxCommandCenter.Views
{
    public partial class LogViewerView : UserControl
    {
        public LogViewerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}