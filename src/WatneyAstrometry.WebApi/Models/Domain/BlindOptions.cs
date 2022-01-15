// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core;

namespace WatneyAstrometry.WebApi.Models.Domain
{
    public class BlindOptions
    {
        public double? MinRadius { get; set; }
        public double? MaxRadius { get; set; }
        public BlindSearchStrategyOptions.RaSearchOrder? RaSearchOrder { get; set; }
        public BlindSearchStrategyOptions.DecSearchOrder? DecSearchOrder { get; set; }
       
    }
}