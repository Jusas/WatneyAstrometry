// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
#pragma warning disable CS1591

namespace WatneyAstrometry.Core.Types
{
    // Using a 406 cell grid.
    // https://arxiv.org/ftp/arxiv/papers/1612/1612.03467.pdf
    public class SkySegmentSphere
    {
        private static readonly List<(double L1, double L2)> LatitudeBands = new List<(double, double)>()
        {
            (80.1375, 90),
            (70.2010, 80.1375),
            (60.1113, 70.2010),
            (50.2170, 60.1113),
            (40.5602, 50.2170),
            (30.1631, 40.5602),
            (20.7738, 30.1631),
            (10.2148, 20.7738),
            (0, 10.2148),
            (-10.2148, 0),
            (-20.7738, -10.2148),
            (-30.1631, -20.7738),
            (-40.5602, -30.1631),
            (-50.2170, -40.5602),
            (-60.1113, -50.2170),
            (-70.2010, -60.1113),
            (-80.1375, -70.2010),
            (-90, -80.1375)
        };

        private static readonly object _mutex = new object();

        private static readonly List<int> CellWidths = new List<int>()
        {
            120,
            40,
            24,
            18,
            15,
            12,
            12,
            10,
            10,
            10,
            10,
            12,
            12,
            15,
            18,
            24,
            40,
            120
        };

        private static List<Cell> _cells;
        private static Cell[,] _cellsArray;

        public static IReadOnlyList<Cell> Cells
        {
            get
            {
                return _cells;
            }
        }

        static SkySegmentSphere()
        {
            _cells = new List<Cell>();
            
            for (var b = 0; b < LatitudeBands.Count; b++)
            {
                var band = LatitudeBands[b];
                var cellWidth = CellWidths[b];
                for (int raLeft = 0, c = 0; raLeft < 360; raLeft += cellWidth, c++)
                {
                    var bounds = new RaDecBounds(raLeft, raLeft + cellWidth, band.L2, band.L1);
                    var cell = new Cell(bounds, b, c);
                    _cells.Add(cell);
                }
            }
            _cellsArray = new Cell[_cells.Max(x => x.BandIndex)+1, _cells.Max(x => x.CellIndex)+1];
            foreach (var cell in _cells)
                _cellsArray[cell.BandIndex, cell.CellIndex] = cell;
        }

        
        public static Cell GetCellAt(EquatorialCoords location)
        {
            var latIndex = 0;
            for (var i = 0; i < LatitudeBands.Count; i++)
            {
                if (location.Dec > LatitudeBands[i].L1 && location.Dec <= LatitudeBands[i].L2)
                {
                    latIndex = i;
                    break;
                }
            }

            var cellIndex = (int)(location.Ra / CellWidths[latIndex]);

            return Cells.First(c => c.BandIndex == latIndex && c.CellIndex == cellIndex);
        }

        public static Cell GetCellByBandAndCellIndex(int band, int cell)
        {
            return _cellsArray[band, cell];
        }

        public static Cell GetCellById(string cellId)
        {
            return Cells.First(c => c.CellId == cellId);
        }
    }
}