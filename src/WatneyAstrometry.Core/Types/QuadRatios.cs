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
        public readonly float R0;
        public readonly float R1;
        public readonly float R2;
        public readonly float R3;
        public readonly float R4;

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

        public static QuadRatios operator *(QuadRatios q, float f) =>
            new QuadRatios(q.R0 * f, q.R1 * f, q.R2 * f, q.R3 * f, q.R4 * f);
    }
}
