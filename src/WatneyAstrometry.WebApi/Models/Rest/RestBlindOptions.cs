// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WatneyAstrometry.Core;
using WatneyAstrometry.WebApi.Models.Domain;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Rest
{
    /// <summary>
    /// Blind solving options.
    /// </summary>
    public class RestBlindOptions
    {

        /// <summary>
        /// The minimum field radius (in degrees) the solver may use in search. Must be > 0.
        /// </summary>
        [DefaultValue(0.25)]
        [FromForm(Name = "minRadius")]
        public double? MinRadius { get; set; }

        /// <summary>
        /// The maximum field radius (in degrees) the solver may use in search. Search starts at max radius, and radius is divided by 2 until min-radius is reached.
        /// </summary>
        [DefaultValue(8.0)]
        [FromForm(Name = "maxRadius")]
        public double? MaxRadius { get; set; }

        /// <summary>
        /// Preferred RA sky scanning order (East or West first).
        /// East(0) == 0..180 degrees RA.
        /// West(1) == 180..360 degrees RA.
        /// </summary>
        [FromForm(Name = "raSearchOrder")]
        public BlindSearchStrategyOptions.RaSearchOrder? RaSearchOrder { get; set; }

        /// <summary>
        /// Preferred Dec sky scanning order (North or South first).
        /// North(0) == 0..90 degrees Dec.
        /// South(1) == -90..0 degrees Dec.
        /// </summary>
        [FromForm(Name = "decSearchOrder")]
        public BlindSearchStrategyOptions.DecSearchOrder? DecSearchOrder { get; set; }


        public class Mappings : AutoMapper.Profile
        {
            public Mappings()
            {
                // Explicit mappings.

                CreateMap<BlindOptions, RestBlindOptions>()
                    .ForMember(dest => dest.MinRadius, x => x.MapFrom(src => src.MinRadius))
                    .ForMember(dest => dest.MaxRadius, x => x.MapFrom(src => src.MaxRadius))
                    .ForMember(dest => dest.RaSearchOrder, x => x.MapFrom(src => src.RaSearchOrder))
                    .ForMember(dest => dest.DecSearchOrder, x => x.MapFrom(src => src.DecSearchOrder))
                    ;

                CreateMap<RestBlindOptions, BlindOptions>()
                    .ForMember(dest => dest.MinRadius, x => x.MapFrom(src => src.MinRadius))
                    .ForMember(dest => dest.MaxRadius, x => x.MapFrom(src => src.MaxRadius))
                    .ForMember(dest => dest.RaSearchOrder, x => x.MapFrom(src => src.RaSearchOrder))
                    .ForMember(dest => dest.DecSearchOrder, x => x.MapFrom(src => src.DecSearchOrder))
                    ;
            }
        }

    }
}