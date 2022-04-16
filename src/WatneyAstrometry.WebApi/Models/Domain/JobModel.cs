// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core;
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
    
}