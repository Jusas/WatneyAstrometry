using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WatneyAstrometry.SolverVizTools.Utils;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
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

        private void Dismiss_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            WindowWorkarounds.ApplyWindowCenteringWorkaround(this);
        }
        
    }
}
