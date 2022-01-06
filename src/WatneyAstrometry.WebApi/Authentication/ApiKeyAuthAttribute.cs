using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WatneyAstrometry.WebApi.Authentication;

[AttributeUsage(validOn: AttributeTargets.Class)]
public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //var configuration = context.HttpContext.RequestServices.GetRequiredService<WatneyApiConfiguration>();
        //if (!"apikey".Equals(configuration.Authentication) || string.IsNullOrEmpty(configuration.ApiKey))
        //{
        //    await next();
        //    return;
        //}

        //bool gotApiKeyFromHeader = context.HttpContext.Request.Headers.TryGetValue("apikey", out var headerApiKey);
        //bool gotApiKeyFromQuery = context.HttpContext.Request.Query.TryGetValue("apikey", out var queryApiKey);
        
        //if (!gotApiKeyFromHeader && !gotApiKeyFromQuery)
        //{
        //    context.Result = new ContentResult()
        //    {
        //        StatusCode = 401,
        //        Content = "Api Key was not provided"
        //    };
        //    return;
        //}

        //var requiredApiKey = configuration.ApiKey;

        //var requestApiKey = gotApiKeyFromHeader ? headerApiKey : queryApiKey;

        //if (!requiredApiKey.Equals(requestApiKey))
        //{
        //    context.Result = new ContentResult()
        //    {
        //        StatusCode = 401,
        //        Content = "Api Key is not valid"
        //    };
        //    return;
        //}

        //await next();
    }
}