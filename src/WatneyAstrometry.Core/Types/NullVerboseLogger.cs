namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// A null logger, which does not log to anywhere.
    /// </summary>
    public class NullVerboseLogger : IVerboseLogger
    {
        /// <inheritdoc/>
        public void Write(string message)
        {
        }

        /// <inheritdoc/>
        public void WriteInfo(string message)
        {
        }

        /// <inheritdoc/>
        public void WriteWarn(string message)
        {
        }

        /// <inheritdoc/>
        public void WriteError(string message)
        {
        }
    }
}