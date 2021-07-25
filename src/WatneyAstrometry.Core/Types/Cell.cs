// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// Class representing a single cell in the grid that covers the sky sphere.
    /// </summary>
    public class Cell
    {
        public RaDecBounds Bounds { get; internal set; }
        
        public int BandIndex { get; internal set; }
        public int CellIndex { get; internal set; }

        public string CellId => $"b{BandIndex:00}c{CellIndex:00}";

        public static string GetCellId(int band, int cell) => $"b{band:00}c{cell:00}";
        
        /// <summary>
        /// Central dec width, so not an absolute; approximate.
        /// </summary>
        public double WidthDeg => EquatorialCoords.GetAngularDistanceBetween(
            new EquatorialCoords(Bounds.RaLeft, Bounds.Center.Dec),
            new EquatorialCoords(Bounds.RaRight, Bounds.Center.Dec));
        public double HeightDeg => Bounds.DecTop - Bounds.DecBottom;

        public RaDecBounds[] SubDivide(int divisions)
        {
            var subCellWidth = (Bounds.RaRight - Bounds.RaLeft) / divisions;
            var subCellHeight = (Bounds.DecTop - Bounds.DecBottom) / divisions;

            var subCells = new RaDecBounds[divisions * divisions];
            int n = 0;

            for (var decBlock = 1; decBlock <= divisions; decBlock++)
            for (var raBlock = 1; raBlock <= divisions; raBlock++)
            {
                subCells[n] = new RaDecBounds(
                    Bounds.RaLeft + (raBlock - 1) * subCellWidth,
                    Bounds.RaLeft + raBlock * subCellWidth,
                    Bounds.DecBottom + decBlock * subCellHeight,
                    Bounds.DecBottom + (decBlock - 1) * subCellHeight);
                n++;
            }

            return subCells;
        }

        public Cell(RaDecBounds bounds, int bandIndex, int cellIndex)
        {
            Bounds = bounds;
            BandIndex = bandIndex;
            CellIndex = cellIndex;
        }
    }
}