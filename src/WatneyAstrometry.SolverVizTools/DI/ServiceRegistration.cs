// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Drawing;
using WatneyAstrometry.SolverVizTools.Services;
using IServiceProvider = WatneyAstrometry.SolverVizTools.Abstractions.IServiceProvider;

namespace WatneyAstrometry.SolverVizTools.DI
{
    internal static class ServiceRegistration
    {
        public static void RegisterServices(IMutableDependencyResolver services, IReadonlyDependencyResolver resolver)
        {
            services.RegisterLazySingleton<IServiceProvider>(() => new ServiceProvider());
            services.RegisterLazySingleton<IDialogProvider>(() => new DialogProvider());
            services.RegisterLazySingleton<ISolveSettingsManager>(() => new SolveSettingsManager());
            services.RegisterLazySingleton<IImageManager>(() => new ImageManager());
            services.RegisterLazySingleton<IVerboseMemoryLogger>(() => new VerboseMemoryLogger());
            services.RegisterLazySingleton<IVisualizer>(() => new Visualizer(resolver.GetService<IServiceProvider>()));
            services.RegisterLazySingleton<IDsoDatabase>(() => new DsoDatabase());
            services.RegisterLazySingleton<IQuadDatabaseDownloadService>(() => new QuadDatabaseDownloadService());

        }
    }
}
