// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// The result of the solver operation.
    /// </summary>
    public class SolveResult
    {
        /// <summary>
        /// The calculated solution, if it was successfully calculated.
        /// </summary>
        public Solution Solution { get; internal set; } = null;
        /// <summary>
        /// Was the solve successful or not.
        /// </summary>
        public bool Success { get; internal set; } = false;
        /// <summary>
        /// Was the solve operation canceled.
        /// </summary>
        public bool Canceled { get; internal set; } = false;
        /// <summary>
        /// The time spent on solving the field.
        /// </summary>
        public TimeSpan TimeSpent { get; internal set; }
        /// <summary>
        /// Areas (segments of sky) searched in the solving process.
        /// </summary>
        public int AreasSearched { get; internal set; } = 0;
        /// <summary>
        /// How many star quad matches were successfully identified.
        /// </summary>
        public int MatchedQuads { get; internal set; } = 0;
        
        // Get the actual matches.
        internal List<StarQuadMatch> MatchInstances { get; set; }

        // Get the detected stars.
        internal IList<ImageStar> DetectedStars { get; set; }

        // Quad density per square deg
        internal int DetectedQuadDensity { get; set; }
        
        /// <summary>
        /// The search run that produced the solution.
        /// </summary>
        public SearchRun SearchRun { get; internal set; }
    }
}