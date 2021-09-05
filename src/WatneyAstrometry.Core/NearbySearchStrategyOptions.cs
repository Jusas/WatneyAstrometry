using System;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// Options for the <see cref="NearbySearchStrategy"/>.
    /// </summary>
    public class NearbySearchStrategyOptions
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
}