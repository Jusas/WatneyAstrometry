// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// A search strategy that simply scans one area, with the given coordinate and field radius.
    /// </summary>
    public class PointSearchStrategy : ISearchStrategy
    {
        /// <summary>
        /// Options for PointSearchStrategy.
        /// </summary>
        public class Options
        {
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

            private float _radiusDegrees = 2;
            /// <summary>
            /// The field radius.
            /// </summary>
            public float RadiusDegrees
            {
                get => _radiusDegrees;
                set
                {
                    if (value <= 0)
                        throw new Exception("RadiusDegrees must be > 0");
                    _radiusDegrees = value;
                }
            }

        }

        private EquatorialCoords _point;
        private Options _options;
        private int[] _densityOffsets;

        /// <summary>
        /// New point search strategy.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public PointSearchStrategy(EquatorialCoords point, Options options)
        {
            _point = point;
            _options = options ?? throw new Exception("Options must be set");
            _densityOffsets = new int[_options.MaxNegativeDensityOffset + _options.MaxPositiveDensityOffset + 1];
            for (int i = -(int)_options.MaxNegativeDensityOffset, n = 0; i <= _options.MaxPositiveDensityOffset; i++, n++)
                _densityOffsets[n] = i;
        }

        /// <inheritdoc />
        public IEnumerable<SearchRun> GetSearchQueue()
        {
            yield return new SearchRun()
            {
                Center = _point,
                RadiusDegrees = _options.RadiusDegrees,
                DensityOffsets = _densityOffsets
            };
        }

        /// <summary>
        /// Does this strategy use parallelism.
        /// </summary>
        public bool UseParallelism => false;
    }
}