// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Image;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// Interface for the plate solver instance.
    /// </summary>
    public interface ISolver
    {
        /// <summary>
        /// Solves the field (image) from an image file on disk. Runs star detection.
        /// </summary>
        /// <param name="filename">The image filename.</param>
        /// <param name="strategy">The solver strategy to use.</param>
        /// <param name="options">Additional options for the solver.</param>
        /// <param name="cancellationToken">Cancellation token, can be used to signal stop to the solver.</param>
        /// <returns>The result of the solver process</returns>
        Task<SolveResult> SolveFieldAsync(string filename, ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Solves the field (image) from an <see cref="IImage"/>. Runs star detection.
        /// </summary>
        /// <param name="image">The image to solve.</param>
        /// <param name="strategy">The solver strategy to use.</param>
        /// <param name="options">Additional options for the solver.</param>
        /// <param name="cancellationToken">Cancellation token, can be used to signal stop to the solver.</param>
        /// <returns>The result of the solver process</returns>
        Task<SolveResult> SolveFieldAsync(IImage image, ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken);
   
        /// <summary>
        /// Solves the field (image) with known dimensions and with a list of already detected stars in the image. Does not run star detection.
        /// </summary>
        /// <param name="imageDimensions">The image dimensions.</param>
        /// <param name="stars">A list of detected stars.</param>
        /// <param name="strategy">The solver strategy to use.</param>
        /// <param name="options">Additional options for the solver.</param>
        /// <param name="cancellationToken">Cancellation token, can be used to signal stop to the solver.</param>
        /// <returns>The result of the solver process</returns>
        Task<SolveResult> SolveFieldAsync(IImageDimensions imageDimensions, IList<ImageStar> stars, ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken);
        
        
    }
}