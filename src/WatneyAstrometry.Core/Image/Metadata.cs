// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.Image
{

    /// <summary>
    /// Metadata of an image that is to be star detected and plate solved.
    /// </summary>
    public class Metadata
    {
        /// <summary>
        /// How many bits per each pixel.
        /// </summary>
        public int BitsPerPixel { get; set; }

        /// <summary>
        /// Image width in pixels.
        /// </summary>
        public int ImageWidth { get; set; }

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        public int ImageHeight { get; set; }

        /// <summary>
        /// The assumed center position if available. In FITS for example, it might be found in the headers.
        /// Return null if not available.
        /// </summary>
        /// <returns></returns>
        public EquatorialCoords CenterPos { get; set; }

        /// <summary>
        /// The assumed field of view (width in degrees, height in degrees) if available. In FITS for example, it might be found in the headers.
        /// Return null if not available.
        /// </summary>
        /// <returns></returns>
        public ViewArea ViewSize { get; set; }

    }
}