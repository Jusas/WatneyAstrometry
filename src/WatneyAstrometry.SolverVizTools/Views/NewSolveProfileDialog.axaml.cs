using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WatneyAstrometry.SolverVizTools.Utils;
using WatneyAstrometry.SolverVizTools.ViewModels;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public partial class NewSolveProfileDialog : Window
    {

        private NewSolveProfileDialogViewModel _viewModel => DataContext as NewSolveProfileDialogViewModel;

        public NewSolveProfileDialog()
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
