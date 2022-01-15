﻿// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.WebApi.Models;
using WatneyAstrometry.WebApi.Models.Domain;

namespace WatneyAstrometry.WebApi.Services;

/// <summary>
/// The manager that is used to submit new jobs, cancel them and to get data of the jobs.
/// </summary>
public interface IJobManager
{
    Task<JobModel> PrepareJob(NewJobInputModel newJobFormModel, IDictionary<string, object> metadata = null);
    Task<JobModel> GetJob(string id);
    Task<JobModel> GetJob(int numericId);
    Task CancelJob(string id);

}