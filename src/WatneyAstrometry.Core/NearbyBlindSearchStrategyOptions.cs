// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// Options for a nearby blind search, <see cref="NearbyBlindSearchStrategy"/>.
    /// </summary>
    public class NearbyBlindSearchStrategyOptions
    {
        /// <summary>
        /// The radius of the search area.
        /// </summary>
        public double SearchAreaRadiusDegrees { get; set; } = 20;

        /// <summary>
        /// The minimum field radius to try.
        /// </summary>
        public double MinFieldRadiusDegrees { get; set; } = 0.25;

        /// <summary>
        /// The maximum field radius to try.
        /// </summary>
        public double MaxFieldRadiusDegrees { get; set; } = 8;

        /// <summary>
        /// How many interpolated intermediate field radii to use in the search.
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
        public uint? IntermediateRadiusSteps { get; set; } = null;

        /// <summary>
        /// Allow the use of parallelism, searching multiple areas simultaneously. <br/>
        /// Therefore defaults to true.
        /// </summary>
        public bool UseParallelism { get; set; } = true;

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