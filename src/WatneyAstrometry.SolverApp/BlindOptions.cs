﻿// Copyright (c) Jussi Saarivirta.
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
        [Option("min-radius", Required = false,
            HelpText = "The minimum field radius (in degrees) the solver may use in search. Must be >= 0.1. If left empty, solver configured default value will be used (default is 0.5)")] // ConstraintValues.MinRecommendedFieldRadius
        public double? MinRadius { get; set; }

        [Option("max-radius", Required = false,
            HelpText = "The maximum field radius (in degrees) the solver may use in search. Must be <= 16. " + // ConstraintValues.MaxRecommendedFieldRadius
                       "Search starts at max radius, and radius is divided by 2 until min-radius is reached. If left empty, solver configured default value will be used (default is 8.0)")]
        public double? MaxRadius { get; set; }

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
        
        [Option('p', "use-parallelism", Required = false,
            HelpText = "Use parallelism, search multiple areas simultaneously. Default is true with blind solves.")]
        public bool? UseParallelism { get; set; }

        [Usage(ApplicationAlias = "watney-solve")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Minimal parameters", new BlindOptions
                {
                    ImageFilename = "andromeda.png"
                });
                yield return new Example("Basic scenario, with defaults", new BlindOptions
                {
                    ImageFilename = "andromeda.png",
                    MinRadius = 0.5,
                    MaxRadius = 8.0
                });
                yield return new Example("Southern hemisphere, west first", new BlindOptions
                {
                    SouthFirst = true,
                    WestFirst = true,
                    ImageFilename = "andromeda.png",
                    MinRadius = 0.5,
                    MaxRadius = 8.0
                });
                yield return new Example("Solve from X,Y list", new BlindOptions
                {
                    XylsFilename = "m31.xyls",
                    XylsImageSize = "1200x700",
                    MinRadius = 0.5,
                    MaxRadius = 8.0
                });
            }
        }

    }
}