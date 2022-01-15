// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Models.Domain;

public class JobParametersModel
{
    public int? MaxStars { get; set; }
    public int? Sampling { get; set; }
    public uint? LowerDensityOffset { get; set; }
    public uint? HigherDensityOffset { get; set; }
    public string Mode { get; set; }
    public NearbyOptions NearbyParameters { get; set; }
    public BlindOptions BlindParameters { get; set; }
}