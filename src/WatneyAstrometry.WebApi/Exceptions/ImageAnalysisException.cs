// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Exceptions;

public class ImageAnalysisException : Exception
{
    public ImageAnalysisException(string message, Exception inner = null) : base(message, inner)
    {
    }
}