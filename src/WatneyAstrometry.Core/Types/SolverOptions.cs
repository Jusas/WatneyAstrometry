// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Options for the solver to use in the solve operation.
    /// </summary>
    public class SolverOptions
    {
        /// <summary>
        /// How many (max) detected stars to use from the image.
        /// If null, use built-in logic to determine a reasonable number.
        /// </summary>
        public int? UseMaxStars { get; set; } = null;

    }
}