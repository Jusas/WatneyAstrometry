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
using IServiceProvider = WatneyAstrometry.SolverVizTools.Abstractions.IServiceProvider;

namespace WatneyAstrometry.SolverVizTools.DI
{
    internal static class ViewModelRegistration
    {

        private static IMutableDependencyResolver _services;
        private static IReadonlyDependencyResolver _resolver;
        private static IViewProvider _viewProvider;

        public static void RegisterViewModels(IMutableDependencyResolver services, IReadonlyDependencyResolver resolver)
        {
            _services = services;
            _resolver = resolver;
            
            _viewProvider = new ViewProvider();
            services.RegisterLazySingleton<IViewProvider>(() => _viewProvider);
            
            
            RegisterLazySingletonViewModel<MainWindowViewModel, MainWindow>(() => 
                new MainWindowViewModel(resolver.GetService<IServiceProvider>()));
            
            RegisterViewModel<NewSolveProfileDialogViewModel, NewSolveProfileDialog>(() => 
                new NewSolveProfileDialogViewModel(resolver.GetService<IServiceProvider>()));

            RegisterViewModel<DsoDatabaseDownloadViewModel, DsoDatabaseDownloadDialog>(() =>
                new DsoDatabaseDownloadViewModel(resolver.GetService<IServiceProvider>()));


            // These ones are not windows, but sub-views (UserControls)

            RegisterLazySingletonViewModel<SettingsPaneViewModel>(() =>
                new SettingsPaneViewModel(resolver.GetService<IServiceProvider>()));

            RegisterLazySingletonViewModel<SettingsManagerViewModel>(() =>
                new SettingsManagerViewModel(resolver.GetService<IServiceProvider>()));

            RegisterLazySingletonViewModel<SolveProcessViewModel>(() =>
                new SolveProcessViewModel(resolver.GetService<IServiceProvider>()));

            
        }


        private static void RegisterLazySingletonViewModel<TViewModel, TView>(
            Func<TViewModel> valueFactory) 
            where TViewModel : ViewModelBase 
            where TView : IControl
        {
            _services.RegisterLazySingleton<TViewModel>(valueFactory);
            _viewProvider.Register<TView, TViewModel>();
        }

        private static void RegisterLazySingletonViewModel<TViewModel>(
            Func<TViewModel> valueFactory)
            where TViewModel : ViewModelBase
        {
            _services.RegisterLazySingleton<TViewModel>(valueFactory);
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
