// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Models.Rest;

/// <summary>
/// Basic message response.
/// </summary>
public class BasicResponse
{
    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; }
}

/// <summary>
/// Internal error
/// </summary>
public class ApiInternalErrorResponse : BasicResponse
{
}


/// <summary>
/// Resource not found
/// </summary>
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

/// <summary>
/// Cancel request response
/// </summary>
public class CancelJobResponse : BasicResponse
{
}