namespace WatneyAstrometry.WebApi.Exceptions;

public class SolverProcessException : Exception
{
    public SolverProcessException(string message, Exception inner = null) : base(message, inner)
    {
    }
}