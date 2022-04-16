// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Image's metadata, the field size.
    /// </summary>
    public class ViewArea
    {
        /// <summary>
        /// Image width, in degrees.
        /// </summary>
        public double WidthDeg { get; set; }
        /// <summary>
        /// Image height, in degrees.
        /// </summary>
        public double HeightDeg { get; set; }
        /// <summary>
        /// Image diameter, in degrees.
        /// </summary>
        public double DiameterDeg => Math.Sqrt(WidthDeg * WidthDeg + HeightDeg * HeightDeg);
    }
}