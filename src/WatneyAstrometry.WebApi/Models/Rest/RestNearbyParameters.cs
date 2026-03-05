// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using WatneyAstrometry.WebApi.Models.Domain;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Rest
{
    /// <summary>
    /// Nearby solve mode options.
    /// </summary>
    public class RestNearbyParameters
    {
        /// <summary>
        /// The search center in RA coordinate.
        /// </summary>
        [Range(0.0, 360.0)]
        [FromForm(Name = "ra")]
        public double? Ra { get; set; }

        /// <summary>
        /// The search center in Dec coordinate.
        /// </summary>
        [Range(-90.0, 90.0)]
        [FromForm(Name = "dec")]
        public double? Dec { get; set; }
        
        /// <summary>
        /// Maximum field radius to try, in degrees.
        /// </summary>
        [FromForm(Name = "maxRadius")]
        public double? MaxFieldRadius { get; set; }

        /// <summary>
        /// Minimum field radius to try, in degrees.
        /// </summary>
        [FromForm(Name = "minRadius")]
        public double? MinFieldRadius { get; set; }

        /// <summary>
        /// How many intermediate steps to use between min-max radius when trying out field radii.
        /// When maxFieldRadius == minFieldRadius this value will be ignored.
        /// If given the value 'auto', the number of intermediate steps will be auto-generated: the
        /// tried field radius value will be halved until minimum value is reached. If not given, value defaults to 0.
        /// </summary>
        [FromForm(Name = "radiusSteps")]
        [DefaultValue("0")]
        public string IntermediateFieldRadiusSteps { get; set; } = "0";

        /// <summary>
        /// Specifies that the assumed center and field radius is provided by the file FITS headers.
        /// Will result in an error if the input file is not FITS or it does not contain the required FITS headers.
        /// </summary>
        [FromForm(Name = "useFitsHeaders")]
        public bool? UseFitsHeaders { get; set; }

        /// <summary>
        /// The search radius (deg), the solver search will cover this area around the center coordinate.
        /// </summary>
        [DefaultValue(20.0)]
        [FromForm(Name = "searchRadius")]
        public double? SearchRadius { get; set; }

        /// <summary>
        /// Convert REST model to job model.
        /// </summary>
        public JobNearbyParameters ToJobNearbyParameters()
        {
            return new JobNearbyParameters()
            {
                Dec = Dec,
                Ra = Ra,
                IntermediateFieldRadiusSteps = string.IsNullOrEmpty(IntermediateFieldRadiusSteps)
                    ? 0
                    : IntermediateFieldRadiusSteps == "auto"
                        ? null
                        : uint.Parse(IntermediateFieldRadiusSteps, CultureInfo.InvariantCulture),
                MaxFieldRadius = MaxFieldRadius,
                MinFieldRadius = MinFieldRadius,
                SearchRadius = SearchRadius,
                UseFitsHeaders = UseFitsHeaders
            };
        }
    }
}