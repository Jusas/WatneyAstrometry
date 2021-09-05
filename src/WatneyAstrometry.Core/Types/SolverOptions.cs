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
        /// <para>
        /// Uses sampling to split the database quads into smaller subsets, running the search per each subset. If a solution can be
        /// found with a subset, less calculations are thrown at finding the solution, making the solve process faster.
        /// </para>
        /// <para>
        /// However, the more subsets we use, the more general overhead of I/O and intermediary calculations we get and at some point
        /// the overhead becomes so big that it eats up the CPU cycles that were otherwise saved. The practical limit probably sits
        /// somewhere near 32. It all depends on the image really.
        /// </para>
        /// <para>
        /// Example: with sampling value 4, only 25% of the quads available in the database are tested. If a solution or an indication
        /// of possible solution is found, we've just saved the time otherwise spent in checking the rest (75%) of the quads in our database.
        /// In most cases this speeds up blind solves significantly.
        /// </para>
        /// <para>
        /// If left as null, the default value of 24 is used.<br/>
        /// A sampling value of 1 effectively means that this feature is turned off.
        /// </para>
        /// </summary>
        public int? UseSampling { get; set; } = null;

    }
}