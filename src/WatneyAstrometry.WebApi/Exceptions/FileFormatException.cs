namespace WatneyAstrometry.WebApi.Exceptions
{
    public class FileFormatException : Exception
    {
        public FileFormatException(string message, Exception inner = null) : base(message, inner)
        {
        }
    }
}
