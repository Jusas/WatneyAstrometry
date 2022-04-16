using System;

namespace WatneyAstrometry.SolverApp.Exceptions;

public class ConfigException : Exception
{
    public ConfigException(string message, Exception inner = null) : base(message, inner)
    {
        
    }
}