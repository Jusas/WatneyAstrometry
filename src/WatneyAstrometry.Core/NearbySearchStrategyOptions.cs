using System;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// Options for the <see cref="NearbySearchStrategy"/>.
    /// </summary>
    public class NearbySearchStrategyOptions
    {
        private double _searchAreaRadius = 10;

        /// <summary>
        /// This is the search area radius in degrees around the center point, this area will be covered
        /// by the search.
        /// </summary>
        public double SearchAreaRadiusDegrees
        {
            get => _searchAreaRadius;
            set
            {
                if (value <= 0)
                    throw new Exception("SearchAreaRadius should be > 0");
                _searchAreaRadius = value;
            }
        }

        private double _maxFieldRadiusDegrees = 2;
        /// <summary>
        /// The maximum telescope field radius to try.
        /// If both <see cref="MinFieldRadiusDegrees"/> and <see cref="MaxFieldRadiusDegrees"/> have the same value, only this field radius will be tried.
        /// </summary>
        public double MaxFieldRadiusDegrees
        {
            get => _maxFieldRadiusDegrees;
            set
            {
                if(value <= 0 || _maxFieldRadiusDegrees > 30)
                    throw new Exception("MaxFieldRadiusDegrees should be > 0 and <= 30");
                _maxFieldRadiusDegrees = value;
            }
        }

        private double _minFieldRadiusDegrees;
        /// <summary>
        /// The minimum telescope field radius to try.
        /// If both <see cref="MinFieldRadiusDegrees"/> and <see cref="MaxFieldRadiusDegrees"/> have the same value, only this field radius will be tried.
        /// </summary>
        public double MinFieldRadiusDegrees
        {
            get => _minFieldRadiusDegrees;
            set
            {
                if (value <= 0)
                    throw new Exception("MinFieldRadiusDegrees should be > 0");
                _minFieldRadiusDegrees = value;
            }
        }

        /// <summary>
        /// How many interpolated intermediate field radii to use in the search.<br/>
        /// If both <see cref="MinFieldRadiusDegrees"/> and <see cref="MaxFieldRadiusDegrees"/> have the same value, this parameter is not used.
        /// <para>
        /// If left as null, the field radius used starts at <see cref="MaxFieldRadiusDegrees"/> and this value is halved
        /// each cycle, until <see cref="MinFieldRadiusDegrees"/> is met. <br/>
        /// <i>Example: null == [8, 4, 2, 1]</i>
        /// </para>
        /// <para>
        /// If set to 0, there will be two search cycles, one with <see cref="MinFieldRadiusDegrees"/> and one with <see cref="MaxFieldRadiusDegrees"/>.
        /// <br/>
        /// <i>Example: 0 == [8, 1]</i>
        /// </para>
        /// <para>
        /// If set > 0, the intermediate field radii used in search will be interpolated.
        /// <br/>
        /// <i>Example: 3 == [8, 6.25, 4.5, 2.75, 1]</i>
        /// </para>
        /// </summary>
        public uint? IntermediateFieldRadiusSteps { get; set; } = null;

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