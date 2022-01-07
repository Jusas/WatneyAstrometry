// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using Newtonsoft.Json;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.WebApi.Controllers.Compatibility.Models;

public class UrlUploadModel
{
    [JsonProperty("session")]
    public string Session { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    // Ignore allow_commercial_use (no relevance here)
    // Ignore allow_modifications (no relevance here)
    // Ignore publicly_visible (no relevance here)
    // Ignore all scale parameters
    
    [JsonProperty("center_ra")]
    public double? CenterRa { get; set; }

    [JsonProperty("center_dec")]
    public double? CenterDec { get; set; }

    [JsonProperty("radius")]
    public double? FieldRadius { get; set; }

    // Use the sampling factor to actually apply Watney Sampling (not downsampling)
    [JsonProperty("downsample_factor")]
    public double? SamplingFactor { get; set; }

    // Ignore tweak_order since we don't use it
    // Ignore use_sextractor because we don't use it
    // Ignore crpix_center because we don't use it
    // Ignore parity because we don't use it
    
    // Image width and height are ignored, because we only support actual images and
    // not xy-coordinate lists.

    // Ignore positional_error since we don't use it

    public bool ValidateModel()
    {
        if (!Url.StartsWith("https://") && !Url.StartsWith("http://"))
            return false;

        //var allowedScaleUnits = new string[]
        //{
        //    "degwidth",
        //    "arcminwidth",
        //    "arcsecperpix"
        //};
        //if (!allowedScaleUnits.Contains(ScaleUnits))
        //    return false;

        if (CenterDec != null && CenterRa != null && FieldRadius != null)
        {
            if (CenterDec < -90 || CenterDec > 90)
                return false;

            if (CenterRa < 0 || CenterRa > 360)
                return false;

            if (FieldRadius <= 0 || FieldRadius > 30)
                return false;
        }

        // Force to 0..16
        if (SamplingFactor != null)
            SamplingFactor = (int)Math.Min(16, Math.Max(0, SamplingFactor.Value));
        
        //var allowedScaleTypes = new string[]
        //{
        //    "ul",
        //    "ev"
        //};
        //if (!allowedScaleTypes.Contains(ScaleType))
        //    return false;
        
        
        return true;
    }

    public EquatorialCoords GetCenterCoords() => CenterRa != null && CenterDec != null
        ? new EquatorialCoords(CenterRa.Value, CenterDec.Value)
        : null;

}