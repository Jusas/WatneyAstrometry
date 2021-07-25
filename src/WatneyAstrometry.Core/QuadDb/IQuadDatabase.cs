// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.QuadDb
{
    /// <summary>
    /// The interface of the star quad database.
    /// </summary>
    public interface IQuadDatabase
    {
        /// <summary>
        /// Gets a list of star quads that are within the given radius from the center.
        /// <para>
        /// The method is given the detected imageQuads to filter out any quads that do not match within small tolerance.
        /// This is to reduce any calculations made down the line.
        /// </para>
        /// <para>
        /// Quad density is given (should be calculated from the image quads) so that quad passes created with lower or higher densities
        /// can be skipped when running comparisons. Quad density offsets can be used to include lower or higher densities, in case
        /// we want more quads to be included in the comparisons.
        /// </para>
        /// </summary>
        /// <param name="center">The center search coordinate.</param>
        /// <param name="radiusDegrees">Radius in degrees around the coordinate.</param>
        /// <param name="quadsPerSqDegree">The reference quads per square degree, for selecting which quad pass to use from the database.</param>
        /// <param name="quadDensityOffsets">Offsets to the quads per square degree density, allowing to include lower and higher densities to the search. The offsets refer to pass indexes. Example: [-1, 0, 1]</param>
        /// <param name="imageQuads">The quads formed from the source image's stars</param>
        Task<List<StarQuad>> GetQuadsAsync(EquatorialCoords center, double radiusDegrees, int quadsPerSqDegree, int[] quadDensityOffsets, ImageStarQuad[] imageQuads);
    }
}
