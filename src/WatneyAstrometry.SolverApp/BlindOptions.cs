// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using WatneyAstrometry.Core;

namespace WatneyAstrometry.SolverApp
{
    [Verb("blind", HelpText = "Perform blind solve.")]
    public class BlindOptions : GenericOptions
    {
        [Option("min-radius", Required = true, Default = 1.0,
            HelpText = "The minimum field radius (in degrees) the solver may use in search. Must be >= 0.1")] // ConstraintValues.MinRecommendedFieldRadius
        public double MinRadius { get; set; }

        [Option("max-radius", Required = true, Default = 8.0,
            HelpText = "The maximum field radius (in degrees) the solver may use in search. Must be <= 16. " + // ConstraintValues.MaxRecommendedFieldRadius
                       "Search starts at max radius, and radius is divided by 2 until min-radius is reached.")]
        public double MaxRadius { get; set; }

        [Option("east-first", Required = false, Default = true,
            HelpText = "Scan Eastern side of the sky first.")]
        public bool EastFirst { get; set; }

        [Option("west-first", Required = false, Default = false,
            HelpText = "Scan Western side of the sky first.")]
        public bool WestFirst { get; set; }

        [Option("north-first", Required = false, Default = true,
            HelpText = "Scan Northern hemisphere first.")]
        public bool NorthFirst { get; set; }

        [Option("south-first", Required = false, Default = false,
            HelpText = "Scan Southern hemisphere first.")]
        public bool SouthFirst { get; set; }
        
        [Option('p', "use-parallelism", Required = false, Default = true,
            HelpText = "Use parallelism, search multiple areas simultaneously.")]
        public bool UseParallelism { get; set; }

        [Usage(ApplicationAlias = "watney-solve")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Basic scenario, with defaults", new BlindOptions
                {
                    ImageFilename = "andromeda.png",
                    MinRadius = 0.5f,
                    MaxRadius = 8.0f
                });
                yield return new Example("Southern hemisphere, west first", new BlindOptions
                {
                    SouthFirst = true,
                    WestFirst = true,
                    ImageFilename = "andromeda.png",
                    MinRadius = 0.5f,
                    MaxRadius = 8.0f
                });
            }
        }

    }
}