// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

#pragma warning disable CS1591

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Represents a latitude band of cells in the sky-segment sphere grid.
    /// Exposes the band's Dec extent and the location of its cells in <see cref="SkySegmentSphere.Cells"/>,
    /// allowing callers to iterate cells band-by-band for Dec/RA optimizations.
    /// </summary>
    public class CellBand
    {
        public double DecBottom { get; }
        public double DecTop { get; }

        /// <summary>RA width of each cell in this band, in whole degrees.</summary>
        public int CellWidthDeg { get; }

        public int CellCount { get; }

        /// <summary>Index of this band's first cell in <see cref="SkySegmentSphere.Cells"/>.</summary>
        public int CellsStartIndex { get; }

        internal CellBand(double decBottom, double decTop, int cellWidthDeg, int cellCount, int cellsStartIndex)
        {
            DecBottom      = decBottom;
            DecTop         = decTop;
            CellWidthDeg   = cellWidthDeg;
            CellCount      = cellCount;
            CellsStartIndex = cellsStartIndex;
        }
    }
}
