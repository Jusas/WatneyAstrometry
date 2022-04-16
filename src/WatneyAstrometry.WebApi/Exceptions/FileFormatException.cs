// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Exceptions
{
    /// <summary>
    /// Exception when file format is incompatible.
    /// </summary>
    public class FileFormatException : Exception
    {
        /// <inheritdoc />
        public FileFormatException(string message, Exception inner = null) : base(message, inner)
        {
        }
    }
}
