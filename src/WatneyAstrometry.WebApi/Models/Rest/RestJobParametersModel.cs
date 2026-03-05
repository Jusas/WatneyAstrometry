using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using WatneyAstrometry.WebApi.Models.Domain;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Rest;

public class RestJobParametersModel //: IValidatableObject
{
    /// <summary>
    /// Maximum number of stars to use from the image. When not given, the solver decides itself.
    /// When given, the solver uses this number. In cases of very high star count present in the image (wide-field images), the solve may fail if this number is not set high enough.
    /// A low number of stars will speed up the solve, since it means less calculations are required, but there's a bigger chance that the solve will fail. 300 is generally a good value.
    /// A high number (> 800) would however also affect performance due to the high number of calculations, and this gets especially noticeable with blind solves.
    /// The hard limit in the solver is set to 800.
    /// </summary>
    [FromForm(Name = "maxStars")]
    public int? MaxStars { get; set; }

    /// <summary>
    /// Try to solve the field using a sampled set of database quads first. With sampling, we try to match
    /// the image's star quads to only a fraction of the available database quads at a time, effectively making the search faster. The idea is that even if we can't find
    /// a solution (enough matching quads), we still get potential matching areas with one or more matching quad, which we can then scan with a full set of database quads
    /// to get the answer faster. Less work is performed in scanning, which makes it faster. Recommended (and default) value to use is 4 but some images may well solve
    /// faster with higher values. Too high values will however result in time wasted in scanning and making the solve actually slower.
    /// </summary>
    [FromForm(Name = "sampling")]
    public int? Sampling { get; set; }

    /// <summary>
    /// Include this many lower quad density passes in search (compared to image quad density).
    /// For practical purposes this is limited to 0 .. 3.
    /// </summary>
    [DefaultValue((uint)1)]
    [FromForm(Name = "lowerDensityOffset")]
    public uint? LowerDensityOffset { get; set; }

    /// <summary>
    /// Include this many higher quad density passes in search (compared to image quad density).
    /// For practical purposes this is limited to 0 .. 3.
    /// </summary>
    [DefaultValue((uint)1)]
    [FromForm(Name = "higherDensityOffset")]
    public uint? HigherDensityOffset { get; set; }
    
    /// <summary>
    /// Solver mode. Supported values are: 'nearby', 'blind'
    /// </summary>
    [FromForm(Name = "mode")]
    [Required]
    [RegularExpression("^(blind|nearby)$")]
    public string Mode { get; set; }

    /// <summary>
    /// The parameters for nearby solving operation.
    /// </summary>
    [FromForm(Name = "nearby")]
    public RestNearbyParameters NearbyParameters { get; set; }

    /// <summary>
    /// The parameters for blind solving operation.
    /// </summary>
    [FromForm(Name = "blind")]
    public RestBlindParameters BlindParameters { get; set; }

    /// <summary>
    /// Convert REST model to job model.
    /// </summary>
    public JobParametersModel ToJobParametersModel()
    {
        return new JobParametersModel()
        {
            HigherDensityOffset = HigherDensityOffset,
            LowerDensityOffset = LowerDensityOffset,
            Mode = Mode,
            MaxStars = MaxStars,
            Sampling = Sampling,
            NearbyParameters = NearbyParameters?.ToJobNearbyParameters(),
            BlindParameters = BlindParameters?.ToJobBlindParameters()
        };
    }

}