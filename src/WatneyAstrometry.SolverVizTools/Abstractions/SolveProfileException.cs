// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;

namespace WatneyAstrometry.SolverVizTools.Abstractions;

public class SolveProfileException : Exception
{
    public SolveProfileException(string message, Exception inner = null) : base(message, inner)
    {
        
    }
}