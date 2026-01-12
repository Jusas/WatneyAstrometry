// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

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
    public RestJobSolutionProperties Solution { get; set; }

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


}