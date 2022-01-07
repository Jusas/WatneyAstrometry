// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Exceptions
{
    public class FileFormatException : Exception
    {
        public FileFormatException(string message, Exception inner = null) : base(message, inner)
        {
        }
    }
}
