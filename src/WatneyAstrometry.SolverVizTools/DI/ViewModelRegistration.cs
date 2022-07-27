// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Splat;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Services;
using WatneyAstrometry.SolverVizTools.ViewModels;
using WatneyAstrometry.SolverVizTools.Views;

namespace WatneyAstrometry.SolverVizTools.DI
{
    internal static class ViewModelRegistration
    {

        private static IMutableDependencyResolver _services;
        private static IReadonlyDependencyResolver _resolver;
        private static IViewProvider _viewProvider;
        private static IDialogProvider _dialogProvider;

        public static void RegisterViewModels(IMutableDependencyResolver services, IReadonlyDependencyResolver resolver)
        {
            _services = services;
            _resolver = resolver;
            _viewProvider = new ViewProvider();
            _dialogProvider = new DialogProvider();

            services.RegisterLazySingleton<IViewProvider>(() => _viewProvider);
            services.RegisterLazySingleton<IDialogProvider>(() => _dialogProvider);

            RegisterLazySingletonViewModel<MainWindowViewModel, MainWindow>(() => 
                new MainWindowViewModel(resolver.GetService<ISolveProfileManager>(), 
                    _viewProvider, 
                    _dialogProvider,
                    resolver.GetService<IImageManager>()));

            // This is a sub-view
            //RegisterLazySingletonViewModel<SettingsManagerViewModel, SettingsManagerView>(() =>
            //    new SettingsManagerViewModel(resolver.GetService<ISolveProfileManager>(), _viewProvider));

            RegisterViewModel<NewSolveProfileDialogViewModel, NewSolveProfileDialog>(() => 
                new NewSolveProfileDialogViewModel(resolver.GetService<ISolveProfileManager>()));
            
        }


        private static void RegisterLazySingletonViewModel<TViewModel, TView>(
            Func<TViewModel> valueFactory) 
            where TViewModel : ViewModelBase 
            where TView : IControl
        {
            _services.RegisterLazySingleton<TViewModel>(valueFactory);
            _viewProvider.Register<TView, TViewModel>();
        }

        private static void RegisterViewModel<TViewModel, TView>(
            Func<TViewModel> valueFactory)
            where TViewModel : ViewModelBase
            where TView : IControl
        {
            _services.Register<TViewModel>(valueFactory);
            _viewProvider.Register<TView, TViewModel>();
        }

    }
}
