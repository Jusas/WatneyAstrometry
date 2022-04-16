// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Represents a star quad match, i.e. a pair of quads, one from the image and one from the quad database.
    /// </summary>
    public class StarQuadMatch
    {
        /// <summary>
        /// The matching quad made of catalog stars.
        /// </summary>
        public StarQuad CatalogStarQuad { get; private set; }
        /// <summary>
        /// The matching quad made of image stars.
        /// </summary>
        public ImageStarQuad ImageStarQuad { get; private set; }
        /// <summary>
        /// The ratio of the quads' largest distances.
        /// </summary>
        public double ScaleRatio => ImageStarQuad.LargestDistance / CatalogStarQuad.LargestDistance;

        /// <summary>
        /// Initialize a new quad match.
        /// </summary>
        /// <param name="catalogStarQuad"></param>
        /// <param name="imageStarQuad"></param>
        public StarQuadMatch(StarQuad catalogStarQuad, ImageStarQuad imageStarQuad)
        {
            CatalogStarQuad = catalogStarQuad;
            ImageStarQuad = imageStarQuad;
        }

        /// <summary>
        /// Comparer that sorts quads based on their scale ration.
        /// </summary>
        public class StarQuadMatchScaleRatioSorter : IComparer<StarQuadMatch>
        {
            /// <inheritdoc/>
            public int Compare(StarQuadMatch x, StarQuadMatch y)
            {
                return x.ScaleRatio < y.ScaleRatio ? -1 : x.ScaleRatio > y.ScaleRatio ? 1 : 0;
            }
        }


    }
}