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

        /// <summary>
        /// Uses sampling to reduce the number of database quads tested against image quads.
        /// Example: with sampling value 4, only 25% of the quads available in the database are tested, the rest are skipped and ignored.
        /// In most cases this speeds up blind solves significantly.
        /// <para>
        /// Recommended to use values 2..10. Best values are probably 4..8.
        /// </para>
        /// </summary>
        public int? UseSampling { get; set; } = null;

    }
}