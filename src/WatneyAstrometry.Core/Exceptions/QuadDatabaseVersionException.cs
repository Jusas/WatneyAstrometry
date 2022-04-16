using System;

namespace WatneyAstrometry.Core.Exceptions
{
    /// <summary>
    /// Quad database version / file format related exception.
    /// </summary>
    public class QuadDatabaseVersionException : Exception
    {
        /// <inheritdoc/>
        public QuadDatabaseVersionException(string message, Exception inner = null) : base(message, inner)
        {
            
        }
    }
}