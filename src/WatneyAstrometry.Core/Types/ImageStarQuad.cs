// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// A star quad formed from the source image's stars.
    /// </summary>
    public class ImageStarQuad : StarQuad
    {
        /// <summary>
        /// The list of stars in the quad.
        /// </summary>
        public override IReadOnlyList<IStar> Stars => ImageStars;
        /// <summary>
        /// The list of stars in the quad.
        /// </summary>
        public IReadOnlyList<ImageStar> ImageStars { get; private set; }
        /// <summary>
        /// The midpoint of the quad, in pixels.
        /// </summary>
        public (double x, double y) PixelMidPoint { get; private set; }

        /// <summary>
        /// Initializes a new quad from image stars.
        /// </summary>
        /// <param name="ratios"></param>
        /// <param name="largestDistance"></param>
        /// <param name="stars"></param>
        /// <exception cref="Exception"></exception>
        public ImageStarQuad(float[] ratios, float largestDistance, IList<ImageStar> stars)
            : base(ratios, largestDistance, null)
        {
            if (stars.Count != 4)
                throw new Exception("A quad has four stars");

            ImageStars = stars.ToList();
            SetMidpoint();
        }

        private void SetMidpoint()
        {
            var x = (ImageStars[0].X + ImageStars[1].X + ImageStars[2].X + ImageStars[3].X) / 4;
            var y = (ImageStars[0].Y + ImageStars[1].Y + ImageStars[2].Y + ImageStars[3].Y) / 4;
            PixelMidPoint = (x, y);
        }
    }
}