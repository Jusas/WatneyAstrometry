// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using AutoMapper;
using WatneyAstrometry.Core;
using WatneyAstrometry.WebApi.Models.Domain;

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
    public JobStatus Status { get; set; }

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


    public class Mappings : AutoMapper.Profile
    {
        public Mappings()
        {
            // Explicit mappings.
            CreateMap<JobModel, RestJobModel>()
                .ValidateMemberList(MemberList.Destination)
                .ForMember(dest => dest.Id, x => x.MapFrom(src => src.Id))
                .ForMember(dest => dest.Parameters, x => x.MapFrom(src => src.Parameters))
                .ForMember(dest => dest.Status, x => x.MapFrom(src => src.Status))
                .ForMember(dest => dest.ImageHeight, x => x.MapFrom(src => src.ImageHeight))
                .ForMember(dest => dest.ImageWidth, x => x.MapFrom(src => src.ImageWidth))
                .ForMember(dest => dest.Solution, x => x.MapFrom(src => src.Solution))
                .ForMember(dest => dest.Updated, x => x.MapFrom(src => src.ImageWidth))
                .ForMember(dest => dest.SolveStarted, x => x.MapFrom(src => src.SolveStarted))
                .ForMember(dest => dest.OriginalFilename, x => x.MapFrom(src => src.OriginalFilename))
                ;

            
        }
    }

}