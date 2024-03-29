﻿// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// A search strategy that scans the given coordinate, and then creates a search pattern
    /// around the coordinate with the given search area radius. Those additional searches are performed
    /// in the order of shorted search area distance from the assumed center.
    /// </summary>
    public class NearbySearchStrategy : ISearchStrategy
    {
     
        /// <summary>
        /// The search center point (aka the assumed correct center coordinate of the telescope view).
        /// </summary>
        public EquatorialCoords SearchCenter { get; internal set; }
        /// <summary>
        /// The search options.
        /// </summary>
        public NearbySearchStrategyOptions Options { get; internal set; }
        /// <summary>
        /// Density offsets, created from <see cref="NearbySearchStrategyOptions.MaxNegativeDensityOffset"/>
        /// and <see cref="NearbySearchStrategyOptions.MaxPositiveDensityOffset"/>.
        /// </summary>
        public int[] DensityOffsets { get; internal set; }

        /// <summary>
        /// An empty constructor; for deserialization purposes.
        /// </summary>
        public NearbySearchStrategy()
        {
        }

        /// <summary>
        /// Create a Nearby search strategy instance with the specified options.
        /// </summary>
        /// <param name="point">The assumed center point of the telescope.</param>
        /// <param name="options">The search options.</param>
        public NearbySearchStrategy(EquatorialCoords point, NearbySearchStrategyOptions options)
        {
            SearchCenter = point;
            Options = options;
            UseParallelism = options.UseParallelism;
            DensityOffsets = new int[Options.MaxNegativeDensityOffset + Options.MaxPositiveDensityOffset + 1];
            for (int i = -(int)Options.MaxNegativeDensityOffset, n = 0; i <= Options.MaxPositiveDensityOffset; i++, n++)
                DensityOffsets[n] = i;
        }

        /// <inheritdoc />
        public IEnumerable<SearchRun> GetSearchQueue()
        {

            if (Options.MaxFieldRadiusDegrees <= 0)
                throw new Exception("MaxFieldRadiusDegrees must be > 0!");

            IEnumerable<double> radiiToTry;
            if(Options.MinFieldRadiusDegrees <= 0)
                radiiToTry = new double[] { Options.MaxFieldRadiusDegrees };
            else if (Options.MaxFieldRadiusDegrees == Options.MinFieldRadiusDegrees)
                radiiToTry = new double[] {Options.MinFieldRadiusDegrees};
            else
                radiiToTry = new double[]
                {
                    Options.MaxFieldRadiusDegrees,
                    Options.MinFieldRadiusDegrees
                };

            if (Options.IntermediateFieldRadiusSteps == null && radiiToTry.Count() > 1)
            {
                var currentRadius = Options.MaxFieldRadiusDegrees;
                var radii = new List<double>() { currentRadius };

                while (currentRadius > Options.MinFieldRadiusDegrees)
                {
                    currentRadius *= 0.5;
                    radii.Add(currentRadius);
                }

                radii.Add(Options.MinFieldRadiusDegrees);
                radiiToTry = radii;
            }
            else if (Options.IntermediateFieldRadiusSteps > 0 && radiiToTry.Count() > 1)
            {
                var delta = Options.MaxFieldRadiusDegrees - Options.MinFieldRadiusDegrees;
                var stepSize = delta / (Options.IntermediateFieldRadiusSteps.Value + 1);
                var currentRadius = Options.MaxFieldRadiusDegrees;
                var radii = new List<double>() { currentRadius };

                while (currentRadius > Options.MinFieldRadiusDegrees)
                {
                    currentRadius -= stepSize;
                    radii.Add(currentRadius);
                }

                radii.Add(Options.MinFieldRadiusDegrees);
                radiiToTry = radii;
            }


            int n = 0;
            var maxDec = Math.Min(SearchCenter.Dec + Options.SearchAreaRadiusDegrees, 90);
            var minDec = Math.Max(SearchCenter.Dec - Options.SearchAreaRadiusDegrees, -90);

            // All search runs, will be from largest radius to smallest, grouped by radius
            // and ordered in group by distance to our search center coordinate.
            var allRuns = new List<SearchRun>();

            foreach (var scopeFieldRadius in radiiToTry)
            {
                var runs = new List<(double distance, SearchRun run)>
                {
                    (0, new SearchRun()
                    {
                        Center = SearchCenter,
                        DensityOffsets = DensityOffsets,
                        RadiusDegrees = (float)scopeFieldRadius
                    })
                };

                // Lazy approach: see what Dec range is within search radius,
                // populate it with semi-overlapping search circles and then measure distance
                // to each circle center and include it if it's in range.
                // Sort circles by distance.

                for (var dec = minDec; dec <= maxDec; dec += scopeFieldRadius, n++) // Take only the decs that are +- radius
                {
                    var angularDistToCover = Math.Cos(Conversions.Deg2Rad(dec)) * 360.0;
                    var numberOfSearchCircles = (int)Math.Ceiling(angularDistToCover / (2 * scopeFieldRadius)) + 1;
                    var raStep = 360.0f / numberOfSearchCircles;
                    var raOffset = n % 2 * 0.5f * raStep;

                    for (var i = 0; i < numberOfSearchCircles; i++)
                    {
                        var ra = (raOffset + i * raStep) % 360.0f; // is this % necessary?
                        var currentSearchCenter = new EquatorialCoords(ra, dec);
                        var distToOriginalSearchCenter = EquatorialCoords.GetAngularDistanceBetween(currentSearchCenter, SearchCenter);
                        if (distToOriginalSearchCenter < Options.SearchAreaRadiusDegrees)
                        {
                            runs.Add((distToOriginalSearchCenter, new SearchRun()
                            {
                                Center = new EquatorialCoords(ra, dec),
                                RadiusDegrees = (float)scopeFieldRadius,
                                DensityOffsets = DensityOffsets
                            }));
                        }
                    }
                }

                allRuns.AddRange(runs.OrderBy(x => x.distance)
                    .Select(x => x.run));
            }

            return allRuns;

        }

        /// <inheritdoc />
        public bool UseParallelism { get; set; }
    }
}