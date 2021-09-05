// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace WatneyAstrometry.Core.Types
{

    public enum SolveRunType
    {
        //NonSampledRun,
        SampledRun,
        SampledHintedRun
        //SampledLastDitchRun
    }
    
    internal class SolveDiagnosticsData
    {
        // Get the actual matches.
        public List<StarQuadMatch> MatchInstances { get; set; }

        // Get the detected stars.
        public IList<ImageStar> DetectedStars { get; set; }

        // How many stars were chosen and used to form quads.
        public int UsedStarCount { get; set; }

        // The run type, did we finally get the solution with a non-sampled, sampled, or some other way.
        public SolveRunType FoundUsingRunType { get; set; }

        // How many areas we scanned that had at least a single matching quad
        public int NumberOfPotentialHitAreasFound { get; set; }

        // Quad density per square deg
        public int DetectedQuadDensity { get; set; }
    }
}