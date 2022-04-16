// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using AutoMapper;
using WatneyAstrometry.Core;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.WebApi.Models.Domain;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Rest;

public class RestJobModel
{
    /// <summary>
    /// The ID of the job.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Job parameters.
    /// </summary>
    public RestJobParametersModel Parameters { get; set; }

    /// <summary>
    /// Job status. Possible values are: Queued, Solving, Success, Failure, Error, Timeout, Canceled
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Image width in pixels.
    /// </summary>
    public int ImageWidth { get; set; }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    public int ImageHeight { get; set; }

    /// <summary>
    /// Solution data when solution is available.
    /// </summary>
    public JobSolutionProperties Solution { get; set; }

    /// <summary>
    /// Timestamp of when the job data was last updated.
    /// </summary>
    public DateTimeOffset Updated { get; set; }

    /// <summary>
    /// Timestamp of when the job solving started. If solve has not started, the value is null.
    /// </summary>
    public DateTimeOffset? SolveStarted { get; set; }

    /// <summary>
    /// The original image filename.
    /// </summary>
    public string OriginalFilename { get; set; }

    /// <summary>
    /// The number of detected stars.
    /// </summary>
    public int? StarsDetected { get; set; }

    /// <summary>
    /// The number of stars used by the solver. This will be available once the solver job has ended.
    /// </summary>
    public int? StarsUsed { get; set; }


    public class Mappings : AutoMapper.Profile
    {
        public Mappings()
        {
            // Explicit mappings.
            CreateMap<JobModel, RestJobModel>()
                .ValidateMemberList(MemberList.Destination)
                .ForMember(dest => dest.Id, x => x.MapFrom(src => src.Id))
                .ForMember(dest => dest.Parameters, x => x.MapFrom(src => src.Parameters))
                .ForMember(dest => dest.Status, x => x.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.ImageHeight, x => x.MapFrom(src => src.ImageHeight))
                .ForMember(dest => dest.ImageWidth, x => x.MapFrom(src => src.ImageWidth))
                .ForMember(dest => dest.Solution, x => x.MapFrom(src => src.Solution))
                .ForMember(dest => dest.Updated, x => x.MapFrom(src => src.Updated))
                .ForMember(dest => dest.SolveStarted, x => x.MapFrom(src => src.SolveStarted))
                .ForMember(dest => dest.OriginalFilename, x => x.MapFrom(src => src.OriginalFilename))
                .ForMember(dest => dest.StarsDetected, x => x.MapFrom(src => src.Stars.Count))
                .ForMember(dest => dest.OriginalFilename, x => x.MapFrom(src => src.OriginalFilename))
                .AfterMap((src, dest) =>
                {
                    if (dest.Solution != null)
                    {
                        dest.Solution.Dec_dms = Conversions.DecDegreesToDdMmSs(dest.Solution.Dec);
                        dest.Solution.Ra_hms = Conversions.RaDegreesToHhMmSs(dest.Solution.Ra);
                    }
                })
                ;

            
        }
    }

}