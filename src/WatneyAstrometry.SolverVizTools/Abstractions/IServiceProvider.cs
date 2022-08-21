// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.SolverVizTools.Abstractions;

public interface IServiceProvider
{
    T GetService<T>();
    T GetAvaloniaService<T>();
}