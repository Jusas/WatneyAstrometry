using System;

namespace WatneyAstrometry.Core.Exceptions
{
    /// <summary>
    /// Quad database related exception.
    /// </summary>
    public class QuadDatabaseException : Exception
    {
        /// <inheritdoc/>
        public QuadDatabaseException(string message, Exception inner = null) : base(message, inner)
        {
            
        }
    }
}