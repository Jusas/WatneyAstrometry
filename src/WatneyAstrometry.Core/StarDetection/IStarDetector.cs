using System.Collections.Generic;
using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.Core.StarDetection
{
    /// <summary>
    /// Interface for the star detector.
    /// </summary>
    public interface IStarDetector
    {
        /// <summary>
        /// Detect stars in the image and return them.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        IList<ImageStar> DetectStars(IImage image);
    }
}