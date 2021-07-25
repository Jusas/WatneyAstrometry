using System;

namespace WatneyAstrometry.Core.Types
{
    public class SolverException : Exception
    {
        public SolverException(string message, Exception inner = null) : base(message, inner)
        {
            
        }
    }
}