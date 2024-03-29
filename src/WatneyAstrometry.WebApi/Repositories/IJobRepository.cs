﻿// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.WebApi.Models;
using WatneyAstrometry.WebApi.Models.Domain;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Repositories;

/// <summary>
/// A repository for job data, stores the full data of a job.
/// </summary>
public interface IJobRepository
{
    Task Insert(JobModel job);
    Task Update(JobModel job);
    Task<JobModel> Get(string id);
    Task<JobModel> Get(int numericId);

}