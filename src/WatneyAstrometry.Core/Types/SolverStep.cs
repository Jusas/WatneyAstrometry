namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Steps the solver will take.
    /// Some steps may never take place (image reading, star detection) if they are
    /// performed outside the Solver.SolveFieldAsync invocation.
    /// </summary>
    public enum SolverStep
    {
        /// <summary>
        /// Started reading the image file.
        /// </summary>
        ImageReadStarted,
        /// <summary>
        /// Finished reading the image file.
        /// </summary>
        ImageReadFinished,
        /// <summary>
        /// Started star detection on the image.
        /// </summary>
        StarDetectionStarted,
        /// <summary>
        /// Finished star detection on the image.
        /// </summary>
        StarDetectionFinished,
        /// <summary>
        /// Quad formation from image stars started.
        /// </summary>
        QuadFormationStarted,
        /// <summary>
        /// Quad formation from image stars finished.
        /// </summary>
        QuadFormationFinished,
        /// <summary>
        /// Started the actual solving of the image (star detection is complete).
        /// </summary>
        SolveProcessStarted,
        /// <summary>
        /// Finished the solving.
        /// </summary>
        SolveProcessFinished
    }
}