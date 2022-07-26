using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public partial class SettingsPaneView : UserControl
    {
        public SettingsPaneView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
