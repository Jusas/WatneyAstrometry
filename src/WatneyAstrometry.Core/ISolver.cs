// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

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
        /// Solves the field (image) from an image file on disk.
        /// </summary>
        /// <param name="filename">The image filename.</param>
        /// <param name="strategy">The solver strategy to use.</param>
        /// <param name="options">Additional options for the solver.</param>
        /// <param name="cancellationToken">Cancellation token, can be used to signal stop to the solver.</param>
        /// <returns>The result of the solver process</returns>
        Task<SolveResult> SolveFieldAsync(string filename, ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Solves the field (image) from an <see cref="IImage"/>.
        /// </summary>
        /// <param name="image">The image instance.</param>
        /// <param name="strategy">The solver strategy to use.</param>
        /// <param name="options">Additional options for the solver.</param>
        /// <param name="cancellationToken">Cancellation token, can be used to signal stop to the solver.</param>
        /// <returns>The result of the solver process</returns>
        Task<SolveResult> SolveFieldAsync(IImage image, ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken);
        
    }
}