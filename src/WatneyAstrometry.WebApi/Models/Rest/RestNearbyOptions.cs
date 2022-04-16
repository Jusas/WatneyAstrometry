// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WatneyAstrometry.WebApi.Models.Domain;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Rest
{
    /// <summary>
    /// Nearby solve mode options.
    /// </summary>
    public class RestNearbyOptions
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


        public class Mappings : AutoMapper.Profile
        {
            public Mappings()
            {
                // Explicit mappings.

                CreateMap<NearbyOptions, RestNearbyOptions>()
                    .ForMember(dest => dest.Ra, x => x.MapFrom(src => src.Ra))
                    .ForMember(dest => dest.Dec, x => x.MapFrom(src => src.Dec))
                    .ForMember(dest => dest.MaxFieldRadius, x => x.MapFrom(src => src.MaxFieldRadius))
                    .ForMember(dest => dest.MinFieldRadius, x => x.MapFrom(src => src.MinFieldRadius))
                    .ForMember(dest => dest.IntermediateFieldRadiusSteps, x => x.MapFrom(new StepsResolverRest()))
                    .ForMember(dest => dest.UseFitsHeaders, x => x.MapFrom(src => src.UseFitsHeaders))
                    .ForMember(dest => dest.SearchRadius, x => x.MapFrom(src => src.SearchRadius))
                    ;

                CreateMap<RestNearbyOptions, NearbyOptions>()
                    .ForMember(dest => dest.Ra, x => x.MapFrom(src => src.Ra))
                    .ForMember(dest => dest.Dec, x => x.MapFrom(src => src.Dec))
                    .ForMember(dest => dest.MaxFieldRadius, x => x.MapFrom(src => src.MaxFieldRadius))
                    .ForMember(dest => dest.MinFieldRadius, x => x.MapFrom(src => src.MinFieldRadius))
                    .ForMember(dest => dest.IntermediateFieldRadiusSteps, x => x.MapFrom(new StepsResolver()))
                    .ForMember(dest => dest.UseFitsHeaders, x => x.MapFrom(src => src.UseFitsHeaders))
                    .ForMember(dest => dest.SearchRadius, x => x.MapFrom(src => src.SearchRadius))
                    ;
            }


            private class StepsResolverRest : IValueResolver<NearbyOptions, RestNearbyOptions, string>
            {
                public string Resolve(NearbyOptions source, RestNearbyOptions destination, string destMember, ResolutionContext context)
                    => source.IntermediateFieldRadiusSteps == null ? "auto" : $"{source.IntermediateFieldRadiusSteps}";
            }

            private class StepsResolver : IValueResolver<RestNearbyOptions, NearbyOptions, uint?>
            {
                public uint? Resolve(RestNearbyOptions source, NearbyOptions destination, uint? destMember, ResolutionContext context)
                {
                    if (string.IsNullOrEmpty(source.IntermediateFieldRadiusSteps))
                        return 0;

                    if (source.IntermediateFieldRadiusSteps == "auto")
                        return null;

                    return uint.Parse(source.IntermediateFieldRadiusSteps, CultureInfo.InvariantCulture);
                }
            }

        }

    }
}