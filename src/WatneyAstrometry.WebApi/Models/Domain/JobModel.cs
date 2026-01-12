// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.WebApi.Models.Rest;

#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Domain;

/// <summary>
/// Domain model for a solver job.
/// </summary>
public class JobModel
{
    public string Id { get; set; }

    // Needed to guarantee compatibility with Astrometry.net; it uses numeric IDs.
    public int NumericId { get; set; }

    public JobParametersModel Parameters { get; set; }
    public List<ImageStar> Stars { get; set; } = new();
    public JobStatus Status { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public JobSolutionProperties Solution { get; set; }
    public DateTimeOffset Updated { get; set; }
    public DateTimeOffset? SolveStarted { get; set; }
    public string OriginalFilename { get; set; }
    public int? StarsUsed { get; set; }

    /// <summary>
    /// Convert job model to REST model.
    /// </summary>
    public RestJobModel ToRestJobModel()
    {
        var restModel = new RestJobModel()
        {
            Id = Id,
            ImageHeight = ImageHeight,
            ImageWidth = ImageWidth,
            Solution = Solution?.ToRestJobSolutionProperties(),
            OriginalFilename = OriginalFilename,
            StarsUsed = StarsUsed,
            SolveStarted = SolveStarted,
            Updated = Updated,
            StarsDetected = Stars.Count,
            Status = Status.ToString(),
            Parameters = Parameters?.ToRestJobParametersModel()
        };
        return restModel;
    }
    
}