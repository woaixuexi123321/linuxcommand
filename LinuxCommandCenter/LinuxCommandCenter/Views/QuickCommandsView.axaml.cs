using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LinuxCommandCenter.Views
{
    public partial class QuickCommandsView : UserControl
    {
        public QuickCommandsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}