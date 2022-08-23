using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WatneyAstrometry.SolverVizTools.Utils;

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

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            WindowWorkarounds.ApplyWindowCenteringWorkaround(this);
        }
    }
}
