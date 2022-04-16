// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.WebApi.Models;
using WatneyAstrometry.WebApi.Models.Domain;

namespace WatneyAstrometry.WebApi.Services;

/// <summary>
/// The manager that is used to submit new jobs, cancel them and to get data of the jobs.
/// </summary>
public interface IJobManager
{
    /// <summary>
    /// Prepare the job (run analysis on the image and queue the job)
    /// </summary>
    /// <param name="newJobFormModel"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    Task<JobModel> PrepareJob(NewJobInputModel newJobFormModel, IDictionary<string, object> metadata = null);
    
    /// <summary>
    /// Get a job by its ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<JobModel> GetJob(string id);

    /// <summary>
    /// Get a job by its numerical ID.
    /// </summary>
    /// <param name="numericId"></param>
    /// <returns></returns>
    Task<JobModel> GetJob(int numericId);

    /// <summary>
    /// Cancel a job.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task CancelJob(string id);

}