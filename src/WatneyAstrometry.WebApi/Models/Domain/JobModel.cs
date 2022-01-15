// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core;

namespace WatneyAstrometry.WebApi.Models.Domain;

public class JobModel
{
    public string Id { get; set; }

    // Needed to guarantee compatibility with Astrometry.net; it uses numeric IDs.
    public int NumericId { get; set; }

    public JobParametersModel Parameters { get; set; }
    public List<ImageStar> Stars { get; set; }
    public JobStatus Status { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public JobSolutionProperties Solution { get; set; }
    public DateTimeOffset Updated { get; set; }
    public DateTimeOffset? SolveStarted { get; set; }
    public string OriginalFilename { get; set; }
    
}