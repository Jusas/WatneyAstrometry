// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Domain
{
    /// <summary>
    /// Domain model for blind solve options.
    /// </summary>
    public class BlindOptions
    {
        public double? MinRadius { get; set; }
        public double? MaxRadius { get; set; }
        public BlindSearchStrategyOptions.RaSearchOrder? RaSearchOrder { get; set; }
        public BlindSearchStrategyOptions.DecSearchOrder? DecSearchOrder { get; set; }
       
    }
}