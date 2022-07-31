using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public partial class DsoDatabaseDownloadDialog : Window
    {
        public DsoDatabaseDownloadDialog()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
