// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// A Solver / solving related exception.
    /// </summary>
    public class SolverException : Exception
    {
        /// <inheritdoc/>
        public SolverException(string message, Exception inner = null) : base(message, inner)
        {
            
        }
    }
}