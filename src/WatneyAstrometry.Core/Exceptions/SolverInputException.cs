using System;

namespace WatneyAstrometry.Core.Exceptions
{
    /// <summary>
    /// Solver input (image, star list) related exception.
    /// </summary>
    public class SolverInputException : Exception
    {
        /// <inheritdoc/>
        public SolverInputException(string message, Exception inner = null) : base(message, inner)
        {
            
        }
    }
}