// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// A search run (a single solve invocation) to perform.
    /// </summary>
    public struct SearchRun
    {
        /// <summary>
        /// The proposed image radius.
        /// </summary>
        public float RadiusDegrees;

        /// <summary>
        /// The proposed image center coordinate.
        /// </summary>
        public EquatorialCoords Center;

        /// <summary>
        /// The quad density offsets (in passes) to use when selecting candidate star quads to include in the quad matching process.
        /// <para>
        /// Example: Calculated quad density in image is 100 quads per square degree. The quad database has passes of 50, 85, 100, and 150 quads per square degree.
        /// [-1, 0, 1] would then include the quads in passes with 85, 100, 150 quads into the search.
        /// </para>
        /// </summary>
        public int[] DensityOffsets;

        // For debugging and logging, to more easily visualize the SearchRun parameters.
        public override string ToString()
        {
            return $"[{Center.Ra}, {Center.Dec}] ({RadiusDegrees:F})";
        }
        
    }

    /// <summary>
    /// A search strategy for the <see cref="Solver"/>.
    /// A strategy defines a list of <see cref="SearchRun"/>s to perform, in sequence or parallel.
    /// </summary>
    public interface ISearchStrategy
    {
        /// <summary>
        /// Returns the search areas in the order of preference.
        /// </summary>
        /// <returns></returns>
        IEnumerable<SearchRun> GetSearchQueue();

        /// <summary>
        /// Tells the solver if running the searches in parallel is allowed.
        /// </summary>
        bool UseParallelism { get; }

    }
}