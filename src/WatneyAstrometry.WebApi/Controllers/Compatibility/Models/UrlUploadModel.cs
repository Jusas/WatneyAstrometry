// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using Newtonsoft.Json;
using WatneyAstrometry.Core.Types;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Controllers.Compatibility.Models;

/// <summary>
/// Model for URL uploads.
/// </summary>
public class UrlUploadModel : UploadModel
{

    [JsonProperty("url")]
    public string Url { get; set; }
    
    public override bool ValidateModel()
    {
        if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
            return false;

        return base.ValidateModel();
    }
    
}