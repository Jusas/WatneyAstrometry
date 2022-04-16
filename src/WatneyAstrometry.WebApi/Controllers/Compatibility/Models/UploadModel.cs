using Newtonsoft.Json;
using WatneyAstrometry.Core.Types;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Controllers.Compatibility.Models;

/// <summary>
/// Base class for file and URL uploads.
/// </summary>
public abstract class UploadModel
{
    [JsonProperty("session")]
    public string Session { get; set; }

    // Ignore allow_commercial_use (no relevance here)
    // Ignore allow_modifications (no relevance here)
    // Ignore publicly_visible (no relevance here)


    [JsonProperty("scale_units")]
    public string ScaleUnits { get; set; }

    [JsonProperty("scale_type")]
    public string ScaleType { get; set; }

    [JsonProperty("scale_lower")]
    public double? ScaleLower { get; set; }

    [JsonProperty("scale_upper")]
    public double? ScaleUpper { get; set; }

    [JsonProperty("scale_est")]
    public double? ScaleEstimate { get; set; }

    [JsonProperty("scale_err")]
    public double? ScaleErrorPercentage { get; set; }


    [JsonProperty("center_ra")]
    public double? CenterRa { get; set; }

    [JsonProperty("center_dec")]
    public double? CenterDec { get; set; }

    [JsonProperty("radius")]
    public double? SearchRadius { get; set; }

    [JsonIgnore]
    public double[] FieldRadius { get; set; }

    [JsonIgnore]
    public uint IntermediateFieldRadiusSteps { get; set; } = 1;

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

    internal Dictionary<string, object> Metadata { get; set; }

    public virtual bool ValidateModel()
    {
        if (SearchRadius is <= 0)
            return false;

        if (CenterDec != null && CenterRa != null)
        {
            if (CenterDec < -90 || CenterDec > 90)
                return false;

            if (CenterRa < 0 || CenterRa > 360)
                return false;

            // Scale 
            if (ScaleType == "ul") // lower and upper
            {
                if (ScaleLower == null || ScaleUpper == null || ScaleLower > ScaleUpper || ScaleLower <= 0)
                    return false;

                FieldRadius = new double[2];

                if (ScaleUnits == "degwidth")
                {
                    FieldRadius[0] = ScaleUpper.Value;
                    FieldRadius[1] = ScaleLower.Value;
                }
                else if (ScaleUnits == "arcminwidth")
                {
                    FieldRadius[0] = ScaleUpper.Value / 60.0;
                    FieldRadius[1] = ScaleLower.Value / 60.0;
                }
                else if (ScaleUnits == "arcsecperpix")
                {
                    // A workaround to the issue that we haven't actually got the image size available here.
                    // It will be calculated later.
                    Metadata = new Dictionary<string, object>()
                    {
                        ["CalculateFieldRadiusFromArcSecsPerPixel"] = new double[] { ScaleUpper.Value, ScaleLower.Value }
                    };
                }
            }
            else if (ScaleType == "ev")
            {
                if (ScaleEstimate == null)
                    return false;

                FieldRadius = new double[1];

                if (ScaleUnits == "degwidth")
                {
                    // Using average, hoping it'll be good enough.
                    FieldRadius[0] = ScaleEstimate.Value;
                }
                else if (ScaleUnits == "arcminwidth")
                {
                    FieldRadius[0] = ScaleEstimate.Value / 60.0;
                }
                else if (ScaleUnits == "arcsecperpix")
                {
                    // A workaround to the issue that we haven't actually got the image size available here.
                    // It will be calculated later.
                    Metadata = new Dictionary<string, object>()
                    {
                        ["CalculateFieldRadiusFromArcSecsPerPixel"] = new double[] { ScaleEstimate.Value }
                    };
                }
            }
            else if (ScaleType != null)
            {
                return false;
            }


        }

        // Force to 0..16
        if (SamplingFactor != null)
            SamplingFactor = (int)Math.Min(16, Math.Max(0, SamplingFactor.Value));

        return true;
    }

    public EquatorialCoords GetCenterCoords() => CenterRa != null && CenterDec != null
        ? new EquatorialCoords(CenterRa.Value, CenterDec.Value)
        : null;

}