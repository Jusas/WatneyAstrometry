// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.IO;
using WatneyAstrometry.Core.Image;

namespace WatneyAstrometry.Core.Fits
{
    /// <summary>
    /// A FITS image instance produced by the <see cref="DefaultFitsReader"/>.
    /// </summary>
    public class FitsImage : IImage
    {
        /// <inheritdoc />
        public Stream PixelDataStream { get; set; }
        /// <inheritdoc />
        public long PixelDataStreamOffset { get; set; }
        /// <inheritdoc />
        public long PixelDataStreamLength { get; set; }
        /// <inheritdoc />
        public Metadata Metadata { get; set; }

        internal List<HduHeaderRecord> HduHeaderRecords { get; set; }

        public void Dispose()
        {
            PixelDataStream?.Dispose();
        }
    }
}