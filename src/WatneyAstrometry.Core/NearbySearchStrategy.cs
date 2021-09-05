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
    /// A search strategy that scans the given coordinate, and then creates a search pattern
    /// around the coordinate with the given search area radius. Those additional searches are performed
    /// in the order of shorted search area distance from the assumed center.
    /// </summary>
    public class NearbySearchStrategy : ISearchStrategy
    {
     
        /// <summary>
        /// The center point (aka the assumed center coordinate of the telescope view).
        /// </summary>
        public EquatorialCoords CenterPoint { get; internal set; }
        /// <summary>
        /// The search options.
        /// </summary>
        public NearbySearchStrategyOptions Options { get; internal set; }
        /// <summary>
        /// Density offsets, created from <see cref="NearbySearchStrategyOptions.MaxNegativeDensityOffset"/> and <see cref="NearbySearchStrategyOptions.MaxPositiveDensityOffset"/>.
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
            CenterPoint = point;
            Options = options;
            UseParallelism = options.UseParallelism;
            DensityOffsets = new int[Options.MaxNegativeDensityOffset + Options.MaxPositiveDensityOffset + 1];
            for (int i = -(int)Options.MaxNegativeDensityOffset, n = 0; i <= Options.MaxPositiveDensityOffset; i++, n++)
                DensityOffsets[n] = i;
        }
        
        /// <inheritdoc />
        public IEnumerable<SearchRun> GetSearchQueue()
        {
            // Lazy approach: see what Dec range is within search radius,
            // populate it with semi-overlapping search circles and then measure distance
            // to each circle center and include it if it's in range.
            // Sort circles by distance.

            var runs = new List<(double distance, SearchRun run)>
            {
                (0, new SearchRun()
                {
                    Center = CenterPoint,
                    DensityOffsets = DensityOffsets,
                    RadiusDegrees = Options.ScopeFieldRadius
                })
            };

            int n = 0;
            float maxDec = Math.Min((float)CenterPoint.Dec + Options.SearchAreaRadius, 90);
            float minDec = Math.Max((float)CenterPoint.Dec - Options.SearchAreaRadius, -90);

            for (float dec = minDec; dec <= maxDec; dec += Options.ScopeFieldRadius, n++) // Take only the decs that are +- radius
            {
                var angularDistToCover = Math.Cos(Conversions.Deg2Rad(dec)) * 360.0;
                var numberOfSearchCircles = (int)Math.Ceiling(angularDistToCover / (2 * Options.ScopeFieldRadius)) + 1;
                var raStep = 360.0f / numberOfSearchCircles;
                var raOffset = n % 2 * 0.5f * raStep;

                for (var i = 0; i < numberOfSearchCircles; i++)
                {
                    var ra = (raOffset + i * raStep) % 360.0f; // is this % necessary?
                    var searchCenter = new EquatorialCoords(ra, dec);
                    var distToOriginalSearchCenter = EquatorialCoords.GetAngularDistanceBetween(searchCenter, CenterPoint);
                    if (distToOriginalSearchCenter < Options.SearchAreaRadius)
                    {
                        runs.Add((distToOriginalSearchCenter, new SearchRun()
                        {
                            Center = new EquatorialCoords(ra, dec),
                            RadiusDegrees = Options.ScopeFieldRadius,
                            DensityOffsets = DensityOffsets
                        }));
                    }
                }
            }

            return runs.OrderBy(x => x.distance)
                .Select(x => x.run)
                .ToList();

        }

        /// <inheritdoc />
        public bool UseParallelism { get; set; }
    }
}