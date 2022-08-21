// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using Avalonia;
using Splat;
using WatneyAstrometry.SolverVizTools.Abstractions;

namespace WatneyAstrometry.SolverVizTools.DI;

/// <summary>
/// This abstracts away the worst of the Splat's locator antipattern.
/// </summary>
public class ServiceProvider : IServiceProvider
{
    public T GetService<T>()
    {
        return Locator.Current.GetService<T>();
    }

    public T GetAvaloniaService<T>()
    {
        return AvaloniaLocator.Current.GetService<T>();
    }
}