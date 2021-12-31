using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace WatneyAstrometry.WebApi.Models;

public class SubmissionDataJsonBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        string fieldName = bindingContext.FieldName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(fieldName);

        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;
        
        bindingContext.ModelState.SetModelValue(fieldName, valueProviderResult);
        
        string value = valueProviderResult.FirstValue;
        if (string.IsNullOrEmpty(value))
            return Task.CompletedTask;

        try
        {
            var result = JsonConvert.DeserializeObject(value, bindingContext.ModelType);
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (Newtonsoft.Json.JsonException)
        {
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}