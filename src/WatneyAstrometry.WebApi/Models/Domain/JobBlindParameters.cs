// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core;
using WatneyAstrometry.WebApi.Models.Rest;

#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Domain
{
    /// <summary>
    /// Domain model for blind solve options.
    /// </summary>
    public class JobBlindParameters
    {
        public double? MinRadius { get; set; }
        public double? MaxRadius { get; set; }
        public BlindSearchStrategyOptions.RaSearchOrder? RaSearchOrder { get; set; }
        public BlindSearchStrategyOptions.DecSearchOrder? DecSearchOrder { get; set; }

        /// <summary>
        /// Convert job model to REST model.
        /// </summary>
        public RestBlindParameters ToRestBlindOptions()
        {
            return new RestBlindParameters()
            {
                DecSearchOrder = DecSearchOrder,
                MaxRadius = MaxRadius,
                MinRadius = MinRadius,
                RaSearchOrder = RaSearchOrder
            };
        }
    }
}