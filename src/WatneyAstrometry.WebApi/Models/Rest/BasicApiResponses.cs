// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Models.Rest;

public class BasicResponse
{
    public string Message { get; set; }
}

public class ApiInternalErrorResponse : BasicResponse
{
}


public class ApiNotFoundResponse : BasicResponse
{

}

/// <summary>
/// Job status response.
/// </summary>
public class ApiStatusModelResponse
{
    /// <summary>
    /// Job status. Possible values are: Queued, Solving, Success, Failure, Error, Timeout, Canceled
    /// </summary>
    public string Status { get; set; }
}

public class CancelJobResponse : BasicResponse
{
}