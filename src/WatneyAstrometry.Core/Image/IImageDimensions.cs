// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Image
{
    public interface IImageDimensions
    {
        /// <summary>
        /// Image width in pixels.
        /// </summary>
        int ImageWidth { get; }

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        int ImageHeight { get; }
    }
}