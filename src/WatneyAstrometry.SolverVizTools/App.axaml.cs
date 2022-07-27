using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Splat;
using WatneyAstrometry.SolverVizTools.ViewModels;
using WatneyAstrometry.SolverVizTools.Views;

namespace WatneyAstrometry.SolverVizTools
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var viewModel = Locator.Current.GetService<MainWindowViewModel>();
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = viewModel // new MainWindowViewModel(),
                };
                viewModel.OwnerWindow = new WindowWrapper(desktop.MainWindow);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
