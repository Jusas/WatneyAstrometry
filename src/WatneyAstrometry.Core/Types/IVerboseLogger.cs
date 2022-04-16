// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// A logger interface for verbose logging.
    /// </summary>
    public interface IVerboseLogger
    {
        /// <summary>
        /// Write text to log.
        /// </summary>
        /// <param name="message"></param>
        [Obsolete("Use WriteInfo/Warn/Error instead")]
        void Write(string message);

        /// <summary>
        /// Write informational text to log.
        /// </summary>
        /// <param name="message"></param>
        void WriteInfo(string message);
        /// <summary>
        /// Write a warning to log.
        /// </summary>
        /// <param name="message"></param>
        void WriteWarn(string message);
        /// <summary>
        /// Write an error to log.
        /// </summary>
        /// <param name="message"></param>
        void WriteError(string message);
    }
}