using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Splat;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.ViewModels;

namespace WatneyAstrometry.SolverVizTools.Views
{
    public partial class MainWindow : Window
    {

        private MainWindowViewModel _viewModel => DataContext as MainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _viewModel?.OnClosing();
        }


        public void ImageSelectionArea_PointerEnter(object sender, PointerEventArgs eventArgs)
        {
            //PointerEnter = "ImageSelectionArea_PointerEnter"
            //FlyoutBase.ShowAttachedFlyout(sender as Control);
        }

        //private void OnSettingsSolveProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var control = sender as ComboBox;
        //    var selectedItem = control.SelectedItem as SolveProfile;
        //    _viewModel.SettingsManagerChosenSolveProfile = selectedItem;
        //}

        //private async void NewSolveProfileButtonClicked(object sender, RoutedEventArgs e)
        //{
        //    var dialog = new NewSolveProfileDialog(); // todo automate this kind of thing like in DS imager?
        //    dialog.DataContext = Locator.Current.GetService<NewSolveProfileDialogViewModel>();
        //    var result = await dialog.ShowDialog<NewSolveProfileDialogViewModel>(this);
        //    // set viewmodel's selected profile...
        //}
        private void AboutButton_OnClick(object sender, RoutedEventArgs e)
        {
            var aboutWin = new AboutWindow();
            aboutWin.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            aboutWin.ShowDialog(this);
        }
    }
}
