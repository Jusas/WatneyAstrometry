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

        public class Options
        {
            private float _searchAreaRadius = 10;

            /// <summary>
            /// This is the search area radius in degrees around the center point, this area will be covered
            /// by the search.
            /// </summary>
            public float SearchAreaRadius
            {
                get => _searchAreaRadius;
                set
                {
                    if (value <= 0)
                        throw new Exception("SearchAreaRadius should be > 0");
                    _searchAreaRadius = value;
                }
            }

            private float _scopeFieldRadius = 2;
            /// <summary>
            /// The assumed telescope field of view radius.
            /// </summary>
            public float ScopeFieldRadius
            {
                get => _scopeFieldRadius;
                set
                {
                    if(value <= 0 || _scopeFieldRadius > 30)
                        throw new Exception("ScopeFieldRadius should be > 0 and <= 30");
                    _scopeFieldRadius = value;
                }
            }

            /// <summary>
            /// Allow the use of parallelism, searching multiple areas simultaneously. <br/>
            /// In the nearby search strategy, result is expected to be reached after very few tries,
            /// and therefore using parallel searches may actually be slower. <br/>
            /// Therefore defaults to false.
            /// </summary>
            public bool UseParallelism { get; set; } = false;

            /// <summary>
            /// Controls the inclusion of lower density quad passes. <br/>
            /// Including more passes in search increases the chance of detection, but also increases solve time.<br/>
            /// Example: Setting this to 2 will include two lower quad density passes in our search (if available in the quad database).
            /// <br/>Defaults to 0.
            /// </summary>
            public uint MaxNegativeDensityOffset { get; set; }

            /// <summary>
            /// Controls the inclusion of higher density quad passes. <br/>
            /// Including more passes in search increases the chance of detection, but also increases solve time.<br/>
            /// Example: Setting this to 2 will include two higher quad density passes in our search (if available in the quad database).
            /// <br/>Defaults to 0.
            /// </summary>
            public uint MaxPositiveDensityOffset { get; set; }
        }

        
        private EquatorialCoords _point;
        private Options _options;
        private int[] _densityOffsets;

        /// <summary>
        /// Create a Nearby search strategy instance.
        /// </summary>
        /// <param name="point">The assumed center point of the telescope.</param>
        /// <param name="options">The search options.</param>
        public NearbySearchStrategy(EquatorialCoords point, Options options)
        {
            _point = point;
            _options = options;
            UseParallelism = options.UseParallelism;
            _densityOffsets = new int[_options.MaxNegativeDensityOffset + _options.MaxPositiveDensityOffset + 1];
            for (int i = -(int)_options.MaxNegativeDensityOffset, n = 0; i <= _options.MaxPositiveDensityOffset; i++, n++)
                _densityOffsets[n] = i;
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
                    Center = _point,
                    DensityOffsets = _densityOffsets,
                    RadiusDegrees = _options.ScopeFieldRadius
                })
            };

            int n = 0;
            float maxDec = Math.Min((float)_point.Dec + _options.SearchAreaRadius, 90);
            float minDec = Math.Max((float)_point.Dec - _options.SearchAreaRadius, -90);

            for (float dec = minDec; dec <= maxDec; dec += _options.ScopeFieldRadius, n++) // Take only the decs that are +- radius
            {
                var angularDistToCover = Math.Cos(Conversions.Deg2Rad(dec)) * 360.0;
                var numberOfSearchCircles = (int)Math.Ceiling(angularDistToCover / (2 * _options.ScopeFieldRadius)) + 1;
                var raStep = 360.0f / numberOfSearchCircles;
                var raOffset = n % 2 * 0.5f * raStep;

                for (var i = 0; i < numberOfSearchCircles; i++)
                {
                    var ra = (raOffset + i * raStep) % 360.0f; // is this % necessary?
                    var searchCenter = new EquatorialCoords(ra, dec);
                    var distToOriginalSearchCenter = EquatorialCoords.GetAngularDistanceBetween(searchCenter, _point);
                    if (distToOriginalSearchCenter < _options.SearchAreaRadius)
                    {
                        runs.Add((distToOriginalSearchCenter, new SearchRun()
                        {
                            Center = new EquatorialCoords(ra, dec),
                            RadiusDegrees = _options.ScopeFieldRadius,
                            DensityOffsets = _densityOffsets
                        }));
                    }
                }
            }

            return runs.OrderBy(x => x.distance)
                .Select(x => x.run)
                .ToList();

        }

        public bool UseParallelism { get; set; }
    }
}