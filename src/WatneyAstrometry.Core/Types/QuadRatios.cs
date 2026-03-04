// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// The five distance ratios that define a star quad, stored as a value type
    /// to avoid heap allocation and pointer indirection on the hot matching path.
    /// </summary>
    public readonly struct QuadRatios
    {
        /// <summary>
        /// Ratio 0
        /// </summary>
        public readonly float R0;
        /// <summary>
        /// Ratio 1
        /// </summary>
        public readonly float R1;
        /// <summary>
        /// Ratio 2
        /// </summary>
        public readonly float R2;
        /// <summary>
        /// Ratio 3
        /// </summary>
        public readonly float R3;
        /// <summary>
        /// Ratio 4
        /// </summary>
        public readonly float R4;

        /// <summary>
        /// Constructor for the QuadRatios struct. The ratios should be precomputed and passed in as parameters and ordered.
        /// R0 should be the smallest ratio and R4 the largest. The constructor does not perform any checks on the input values, so it is the caller's responsibility to ensure they are valid and ordered correctly.
        /// </summary>
        /// <param name="r0">The smallest ratio.</param>
        /// <param name="r1">The second smallest ratio.</param>
        /// <param name="r2">The middle ratio.</param>
        /// <param name="r3">The second largest ratio.</param>
        /// <param name="r4">The largest ratio.</param>
        public QuadRatios(float r0, float r1, float r2, float r3, float r4)
        {
            R0 = r0; R1 = r1; R2 = r2; R3 = r3; R4 = r4;
        }

        /// <summary>
        /// Indexer for non-performance-critical access (equality comparers, visualizers).
        /// Use the R0–R4 fields directly in hot paths to avoid the branch.
        /// </summary>
        public float this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return R0;
                    case 1: return R1;
                    case 2: return R2;
                    case 3: return R3;
                    default: return R4;
                }
            }
        }

        /// <summary>
        /// Multiplies all ratios in the QuadRatios struct by a given float factor. This can be used to scale the ratios for various purposes, such as adjusting for different distance scales or normalizing the values. The resulting QuadRatios struct will have each ratio multiplied by the specified factor, maintaining the same order and relative proportions between the ratios.
        /// </summary>
        /// <param name="q">The QuadRatios struct to be scaled.</param>
        /// <param name="f">The float factor by which to multiply each ratio.</param>
        /// <returns>A new QuadRatios struct with each ratio multiplied by the specified factor.</returns>
        public static QuadRatios operator *(QuadRatios q, float f) =>
            new QuadRatios(q.R0 * f, q.R1 * f, q.R2 * f, q.R3 * f, q.R4 * f);
    }
}
