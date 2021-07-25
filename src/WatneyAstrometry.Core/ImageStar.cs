﻿using System;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// A detected star in an image, with its calculated center location in pixels
    /// and its brightness represented in the value range if the image's pixel format.
    /// </summary>
    public class ImageStar : IStar
    {
        /// <summary>
        /// The star center X coordinate (pixels).
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// The star center Y coordinate (pixels).
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// The star brightness represented in the value range if the image's pixel format.
        /// </summary>
        public long Brightness { get; set; }

        public ImageStar()
        {
        }

        public ImageStar(double x, double y, long brightness)
        {
            X = x;
            Y = y;
            Brightness = brightness;
        }

        /// <summary>
        /// Returns distance to another star in the image.
        /// </summary>
        /// <param name="anotherStar">Another ImageStar</param>
        /// <returns></returns>
        public double CalculateDistance(IStar anotherStar)
        {
            ImageStar s = (ImageStar) anotherStar;
            var deltaX = X - s.X;
            var deltaY = Y - s.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
}