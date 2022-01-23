// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;

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
        public Solution Solution { get; set; } = null;
        /// <summary>
        /// Was the solve successful or not.
        /// </summary>
        public bool Success { get; set; } = false;
        /// <summary>
        /// Was the solve operation canceled.
        /// </summary>
        public bool Canceled { get; set; } = false;
        /// <summary>
        /// The time spent on solving the field.
        /// </summary>
        public TimeSpan TimeSpent { get; set; }
        /// <summary>
        /// Areas (segments of sky) searched in the solving process.
        /// </summary>
        public int AreasSearched { get; set; } = 0;
        /// <summary>
        /// How many star quad matches were successfully identified.
        /// </summary>
        public int MatchedQuads { get; set; } = 0;

        /// <summary>
        /// How many stars were detected or given as input to the solver.
        /// </summary>
        public int StarsDetected { get; set; } = 0;
        /// <summary>
        /// How many stars were used by the solver to form quads.
        /// </summary>
        public int StarsUsedInSolve { get; set; } = 0;

        internal SolveDiagnosticsData DiagnosticsData { get; set; }

        //// Get the actual matches.
        //internal List<StarQuadMatch> MatchInstances { get; set; }

        //// Get the detected stars.
        //internal IList<ImageStar> DetectedStars { get; set; }

        //internal int UsedStarCount { get; set; }
        //internal RunType FoundUsingRunType { get; set; }
        //internal int NumberOfPotentialHitAreasFound { get; set; }

        //// Quad density per square deg
        //internal int DetectedQuadDensity { get; set; }

        /// <summary>
        /// For sampling; we will want to know those runs that had potential for a match.
        /// Sampling may not get a full match (enough quads), and we will repeat the search
        /// without sampling for promising candidates first, and if still no full match is
        /// found we will repeat the search for all areas without sampling.
        /// </summary>
        internal int NumPotentialMatches { get; set; }

        /// <summary>
        /// The search run that produced the solution.
        /// </summary>
        public SearchRun SearchRun { get; set; }
    }
}