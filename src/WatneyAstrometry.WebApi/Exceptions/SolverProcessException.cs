// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.WebApi.Exceptions;

public class SolverProcessException : Exception
{
    public SolverProcessException(string message, Exception inner = null) : base(message, inner)
    {
    }
}