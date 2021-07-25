using System.Collections.Generic;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Represents a star quad match, i.e. a pair of quads, one from the image and one from the quad database.
    /// </summary>
    public class StarQuadMatch
    {
        public StarQuad CatalogStarQuad { get; private set; }
        public ImageStarQuad ImageStarQuad { get; private set; }
        public double ScaleRatio => ImageStarQuad.LargestDistance / CatalogStarQuad.LargestDistance;

        public StarQuadMatch(StarQuad catalogStarQuad, ImageStarQuad imageStarQuad)
        {
            CatalogStarQuad = catalogStarQuad;
            ImageStarQuad = imageStarQuad;
        }

        public class StarQuadMatchScaleRatioSorter : IComparer<StarQuadMatch>
        {
            public int Compare(StarQuadMatch x, StarQuadMatch y)
            {
                return x.ScaleRatio < y.ScaleRatio ? -1 : x.ScaleRatio > y.ScaleRatio ? 1 : 0;
            }
        }


    }
}