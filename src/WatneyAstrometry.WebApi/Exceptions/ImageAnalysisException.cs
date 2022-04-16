// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Exceptions;

/// <summary>
/// Exception in the image analysis.
/// </summary>
public class ImageAnalysisException : Exception
{
    /// <inheritdoc />
    public ImageAnalysisException(string message, Exception inner = null) : base(message, inner)
    {
    }
}