using Microsoft.AspNetCore.Mvc.ModelBinding;
using WatneyAstrometry.WebApi.Models;

namespace WatneyAstrometry.WebApi.Utils;

public static class ModelValidation
{
    public static ApiBadRequestErrorResponse ProduceErrorResponse(this ModelStateDictionary modelState)
    {
        ApiBadRequestErrorResponse errorResponse = new ApiBadRequestErrorResponse();
        errorResponse.Message = "Input was not valid.";
        var errors = new List<string>();
        foreach (var item in modelState.Values)
        {
            var errorMessage = string.Join("; ", item.Errors.Select(err => err.ErrorMessage));
            if(!string.IsNullOrEmpty(errorMessage))
                errors.Add(errorMessage);
        }

        errorResponse.Errors = errors.ToArray();
        return errorResponse;
    }
}