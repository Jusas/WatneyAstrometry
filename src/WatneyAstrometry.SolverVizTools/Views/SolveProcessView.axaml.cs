using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public partial class SolveProcessView : UserControl
    {
        public SolveProcessView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
