// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Watney Solver configuration with a global level settings, affecting all solver instances.
    /// </summary>
    public class SolverGlobalConfiguration
    {
        /// <summary>
        /// Maximum number of threads to use. This controls concurrency.
        /// <br/>
        /// For maximum solver performance, <see cref="Environment.ProcessorCount"/> is recommended, and is the default value.
        /// <br/>
        /// However when using the solver inside a UI application for example, it's recommended to set this to 
        /// <see cref="Environment.ProcessorCount"/> - 1 or lower in order to allow the UI to update and to give other
        /// tasks some CPU time.
        /// </summary>
        public int MaxThreads { get; set; }

        /// <summary>
        /// The default SolverGlobalConfiguration.
        /// </summary>
        public static SolverGlobalConfiguration Default => new SolverGlobalConfiguration
        {
            MaxThreads = Environment.ProcessorCount
        };
    }
}