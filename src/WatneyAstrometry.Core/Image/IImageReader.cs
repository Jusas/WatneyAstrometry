// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace WatneyAstrometry.Core.Image
{
    /// <summary>
    /// Standard interface for image readers.
    /// <para>
    /// The image reader should read an image file or a stream, and then return
    /// an <see cref="IImage"/> reference that contains a readable <see cref="Stream"/> and the image <see cref="Metadata"/>.
    /// </para>
    /// <para>
    /// The implementation can support one or more file formats, and is registered to the <see cref="Solver"/> so that it may use it
    /// to load the solvable images when a solve operation is requested.
    /// </para>
    /// </summary>
    public interface IImageReader
    {
        /// <summary>
        /// Reads an image from a file. Called by the solver when given a file with
        /// matching file extension.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        IImage FromFile(string filename);

        /// <summary>
        /// Reads an image from a stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        IImage FromStream(Stream stream);


    }
}