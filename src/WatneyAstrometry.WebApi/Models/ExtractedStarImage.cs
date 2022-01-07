// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.WebApi.Models;

public class ExtractedStarImage : IImageDimensions
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
}