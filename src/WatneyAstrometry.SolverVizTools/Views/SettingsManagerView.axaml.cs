using Avalonia;
using Avalonia.Controls;
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

        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var n = nameof(_viewModel.SelectedProfile);
            //_viewModel.RaisePropertyChanged(nameof(_viewModel.SelectedProfile));
        }
    }
}
