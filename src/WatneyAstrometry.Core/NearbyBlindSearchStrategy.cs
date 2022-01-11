// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// A nearby (only search nearby area) blind search strategy.
    /// Only a specified area is searched so a center coordinate and search radius is given
    /// but it is a "blind" search meaning that the scope's field radius is not known and
    /// therefore different values need to be tried.
    /// </summary>
    public class NearbyBlindSearchStrategy : ISearchStrategy
    {
        /// <summary>
        /// The center point where the search will start.
        /// </summary>
        public EquatorialCoords SearchCenter { get; internal set; }
        /// <summary>
        /// The search options.
        /// </summary>
        public NearbyBlindSearchStrategyOptions Options { get; internal set; }
        /// <summary>
        /// Density offsets, created from <see cref="NearbyBlindSearchStrategyOptions.MaxNegativeDensityOffset"/>
        /// and <see cref="NearbyBlindSearchStrategyOptions.MaxPositiveDensityOffset"/>.
        /// </summary>
        public int[] DensityOffsets { get; internal set; }

        /// <summary>
        /// An empty constructor; for deserialization purposes.
        /// </summary>
        public NearbyBlindSearchStrategy()
        {
        }

        /// <summary>
        /// Create a Nearby Blind search strategy instance with the specified options.
        /// </summary>
        /// <param name="point">The assumed center point of the telescope.</param>
        /// <param name="options">The search options.</param>
        public NearbyBlindSearchStrategy(EquatorialCoords point, NearbyBlindSearchStrategyOptions options)
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
           
            IEnumerable<double> radiiToTry = new double[]
            {
                Options.MaxFieldRadiusDegrees,
                Options.MinFieldRadiusDegrees
            };

            if (Options.IntermediateRadiusSteps == null)
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
            else if (Options.IntermediateRadiusSteps > 0)
            {
                var delta = Options.MaxFieldRadiusDegrees - Options.MinFieldRadiusDegrees;
                var stepSize = delta / (Options.IntermediateRadiusSteps.Value + 1);
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