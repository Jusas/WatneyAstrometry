namespace WatneyAstrometry.WebApi.Models;

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

public class ApiStatusModelResponse
{
    public string Status { get; set; }
}

public class CancelJobResponse : BasicResponse
{
}