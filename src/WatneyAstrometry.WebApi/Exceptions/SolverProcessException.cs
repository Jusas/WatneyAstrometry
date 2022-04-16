// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Exceptions;

/// <summary>
/// Exception in solving process.
/// </summary>
public class SolverProcessException : Exception
{
    /// <inheritdoc />
    public SolverProcessException(string message, Exception inner = null) : base(message, inner)
    {
    }
}