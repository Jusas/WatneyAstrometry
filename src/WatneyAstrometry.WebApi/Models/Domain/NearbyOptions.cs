// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

#pragma warning disable CS1591
namespace WatneyAstrometry.WebApi.Models.Domain
{
    /// <summary>
    /// Domain model for nearby options.
    /// </summary>
    public class NearbyOptions
    {
        public double? Ra { get; set; }
        public double? Dec { get; set; }
        public double? MaxFieldRadius { get; set; }
        public double? MinFieldRadius { get; set; }
        public uint? IntermediateFieldRadiusSteps { get; set; }

        public bool? UseFitsHeaders { get; set; }
        public double? SearchRadius { get; set; }
        
    }
}