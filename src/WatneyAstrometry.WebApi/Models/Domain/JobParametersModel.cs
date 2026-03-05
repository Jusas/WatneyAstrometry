// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.WebApi.Models.Rest;

#pragma warning disable CS1591
namespace WatneyAstrometry.WebApi.Models.Domain;

/// <summary>
/// Domain model for job parameters.
/// </summary>
public class JobParametersModel
{
    public int? MaxStars { get; set; }
    public int? Sampling { get; set; }
    public uint? LowerDensityOffset { get; set; }
    public uint? HigherDensityOffset { get; set; }
    public string Mode { get; set; }
    public JobNearbyParameters NearbyParameters { get; set; }
    public JobBlindParameters BlindParameters { get; set; }

    /// <summary>
    /// Convert job model to REST model.
    /// </summary>
    public RestJobParametersModel ToRestJobParametersModel()
    {
        return new RestJobParametersModel
        {
            MaxStars = MaxStars,
            Sampling = Sampling,
            LowerDensityOffset = LowerDensityOffset,
            HigherDensityOffset = HigherDensityOffset,
            Mode = Mode,
            NearbyParameters = NearbyParameters?.ToRestNearbyOptions(),
            BlindParameters = BlindParameters?.ToRestBlindOptions()
        };
    }
}