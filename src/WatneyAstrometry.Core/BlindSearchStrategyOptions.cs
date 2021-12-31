// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// Options for the <see cref="BlindSearchStrategy"/>.
    /// </summary>
    public class BlindSearchStrategyOptions
    {
        /// <summary>
        /// In which order do we want to search RA.
        /// </summary>
        public enum RaSearchOrder
        {
            /// <summary>
            /// Within Northern or Southern half, start search from the East side (0..180 degrees), and once East has
            /// been fully searched, switch to West side (180..360 degrees).
            /// </summary>
            EastFirst,
            /// <summary>
            /// Within Northern or Southern half, start search from the West side (180..360 degrees), and once West has
            /// been fully searched, switch to East side (0..180 degrees).
            /// </summary>
            WestFirst
        }

        /// <summary>
        /// In which order do we want to search Dec.
        /// </summary>
        public enum DecSearchOrder
        {
            /// <summary>
            /// Search 0..90 degrees declination first, searching RA in <see cref="RaSearchOrder"/>.
            /// </summary>
            NorthFirst,
            /// <summary>
            /// Search -90..0 degrees declination first, searching RA in <see cref="RaSearchOrder"/>.
            /// </summary>
            SouthFirst
        }
    
        private float _startRadiusDegrees = 22.5f;

        /// <summary>
        /// This is the starting search area radius in degrees, i.e. the full sky
        /// will get searched in circles of this radius. If matches are not found,
        /// the search then continues on by halving the radius over and over,
        /// until the search completes or is stopped.
        /// </summary>
        public float StartRadiusDegrees
        {
            get => _startRadiusDegrees;
            set
            {
                if (value > 30 || value <= 0)
                    throw new Exception("Start radius should be > 0 and <= 30 degrees");
                _startRadiusDegrees = value;
            }
        }

        private float _minRadiusDegrees = 22.5f / 32;

        /// <summary>
        /// Minimum radius for the search.
        /// </summary>
        public float MinRadiusDegrees
        {
            get => _minRadiusDegrees;
            set
            {
                if (value <= 0 || value > 30)
                    throw new Exception("Minimum radius must be > 0 and <= 30");
                _minRadiusDegrees = value;
            }
        }

        /// <summary>
        /// Which side of the sky sphere to start with.
        /// </summary>
        public RaSearchOrder SearchOrderRa { get; set; } = RaSearchOrder.EastFirst;
        /// <summary>
        /// Which pole of the sky sphere to start with.
        /// </summary>
        public DecSearchOrder SearchOrderDec { get; set; } = DecSearchOrder.NorthFirst;
        
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

        /// <summary>
        /// Allow the use of parallelism, searching multiple areas simultaneously. <br/>
        /// This may use more memory, but is significantly faster. <br/>
        /// Defaults to true.
        /// </summary>
        public bool UseParallelism { get; set; } = true;


    }
}