// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Models;

public enum JobStatus
{
    Queued = 0,
    Solving = 1,
    Success = 2,
    Failure = 3,
    Error = 4,
    Timeout = 5,
    Canceled = 6
}