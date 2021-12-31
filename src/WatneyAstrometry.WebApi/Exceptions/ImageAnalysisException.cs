namespace WatneyAstrometry.WebApi.Exceptions;

public class ImageAnalysisException : Exception
{
    public ImageAnalysisException(string message, Exception inner = null) : base(message, inner)
    {
    }
}