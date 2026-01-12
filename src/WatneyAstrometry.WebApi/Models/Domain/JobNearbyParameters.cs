// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.WebApi.Models.Rest;

#pragma warning disable CS1591
namespace WatneyAstrometry.WebApi.Models.Domain
{
    /// <summary>
    /// Domain model for nearby options.
    /// </summary>
    public class JobNearbyParameters
    {
        public double? Ra { get; set; }
        public double? Dec { get; set; }
        public double? MaxFieldRadius { get; set; }
        public double? MinFieldRadius { get; set; }
        public uint? IntermediateFieldRadiusSteps { get; set; }

        public bool? UseFitsHeaders { get; set; }
        public double? SearchRadius { get; set; }

        /// <summary>
        /// Convert job model to REST model.
        /// </summary>
        public RestNearbyParameters ToRestNearbyOptions()
        {
            return new RestNearbyParameters()
            {
                Dec = Dec,
                Ra = Ra,
                MaxFieldRadius = MaxFieldRadius,
                MinFieldRadius = MinFieldRadius,
                SearchRadius = SearchRadius,
                UseFitsHeaders = UseFitsHeaders,
                IntermediateFieldRadiusSteps = IntermediateFieldRadiusSteps == null
                    ? "auto"
                    : $"{IntermediateFieldRadiusSteps}"
            };
        }
        
    }
}