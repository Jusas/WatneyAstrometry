// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia.MaterialDesign;
using System;
using System.Globalization;
using System.Threading;
using Splat;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.DI;
using WatneyAstrometry.SolverVizTools.Services;
using WatneyAstrometry.SolverVizTools.ViewModels;

namespace WatneyAstrometry.SolverVizTools
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            RegisterDependencies(Locator.CurrentMutable, Locator.Current);

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        } 

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI()
                .WithIcons(container => container
                    .Register<MaterialDesignIconProvider>()
                    .Register<FontAwesomeIconProvider>()
                );

        public static void RegisterDependencies(IMutableDependencyResolver services, IReadonlyDependencyResolver resolver)
        {
            ViewModelRegistration.RegisterViewModels(services, resolver);
            ServiceRegistration.RegisterServices(services, resolver);
        }
    }
}
