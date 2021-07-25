using System.Collections.Generic;
using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.Core.StarDetection
{
    /// <summary>
    /// Interface for filtering stars after initial star detection.
    /// </summary>
    public interface IStarDetectionFilter
    {
        /// <summary>
        /// Apply filtering for the star pixel bins and return a new filtered list.
        /// </summary>
        /// <param name="starPixelBins"></param>
        /// <param name="imageMetadata"></param>
        /// <returns></returns>
        List<StarPixelBin> ApplyFilter(IReadOnlyList<StarPixelBin> starPixelBins, Metadata imageMetadata);
    }
}