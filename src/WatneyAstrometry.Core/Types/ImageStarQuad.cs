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
        public override IReadOnlyList<IStar> Stars => ImageStars;
        public IReadOnlyList<ImageStar> ImageStars { get; private set; }
        public (double x, double y) PixelMidPoint { get; private set; }

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