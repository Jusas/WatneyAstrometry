using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using WatneyAstrometry.SolverVizTools.ViewModels;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public partial class SettingsManagerView : UserControl
    {

        private SettingsManagerViewModel _viewModel => DataContext as SettingsManagerViewModel;

        public SettingsManagerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void QuadDatabasePathControl_OnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property.Name == "Text")
                _viewModel.WatneyConfiguration.RaisePropertyChanged(nameof(_viewModel.WatneyConfiguration.IsValidQuadDatabasePath));
        }


    }
}
