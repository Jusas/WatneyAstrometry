// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// A logger interface for verbose logging.
    /// </summary>
    public interface IVerboseLogger
    {
        void Write(string message);
    }
}