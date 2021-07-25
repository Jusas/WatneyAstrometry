// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;

namespace WatneyAstrometry.Core.Types
{
    public class SolverException : Exception
    {
        public SolverException(string message, Exception inner = null) : base(message, inner)
        {
            
        }
    }
}