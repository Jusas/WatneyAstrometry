// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;

namespace WatneyAstrometry.Core.Image
{
    /// <summary>
    /// An interface to an image that is fed to the <see cref="Solver"/>.
    /// <para>
    /// The image should be a monochrome image (color images should be mutated into monochrome)
    /// and a contiguous block of bytes should be provided in the <see cref="IImage.PixelDataStream"/> for the solver
    /// to use.
    /// </para>
    /// </summary>
    public interface IImage : IDisposable
    {
        /// <summary>
        /// A stream containing the image pixels (monochrome) from top-left to bottom-right.
        /// </summary>
        Stream PixelDataStream { get; }

        /// <summary>
        /// A byte offset to the pixel data, if the pixel data does not begin from first byte.
        /// </summary>
        long PixelDataStreamOffset { get; }

        /// <summary>
        /// The length of the pixel data in bytes.
        /// </summary>
        long PixelDataStreamLength { get; }

        /// <summary>
        /// The image's metadata (width, height, BPP + others).
        /// </summary>
        Metadata Metadata { get; }
    }
}