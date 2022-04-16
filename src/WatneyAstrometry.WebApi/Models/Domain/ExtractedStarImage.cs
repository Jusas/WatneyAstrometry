// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core.Image;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Models.Domain;

public class ExtractedStarImage : IImageDimensions
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
}