// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.QuadDb
{
    /// <summary>
    /// Class representing a set of Cell files. The quad database is split into cell files.
    /// </summary>
    public class QuadDatabaseCellFileSet : IDisposable
    {
        // TODO internal or public?
        // Group descriptors by cellId, and create a new CellFile per descriptor
        private class CellFilePassDensity
        {
            public float QuadsPerSqDeg { get; set; }
            public int FileIndex { get; set; }
            public int PassIndex { get; set; }
        }

        private CellFilePassDensity[] _cellFilePassDensities = new CellFilePassDensity[0];
        
        private QuadDatabaseCellFile[] _sourceFiles = new QuadDatabaseCellFile[0];


        private bool _disposing;
        public string CellId { get; }
        public Cell CellReference { get; }


        public static QuadDatabaseCellFileSet[] FromIndexes(QuadDatabaseCellFileIndex[] indexes)
        {
            var sets = new QuadDatabaseCellFileSet[SkySegmentSphere.Cells.Count];
            var allCellFiles = indexes.SelectMany(i => i.CellFiles)
                .GroupBy(i => i.Descriptor.CellId);

            int n = 0;
            foreach (var fileGroup in allCellFiles)
            {
                var set = new QuadDatabaseCellFileSet(fileGroup.Key, fileGroup.ToArray());
                sets[n++] = set;
            }

            return sets;
        }
        



        private QuadDatabaseCellFileSet(string cellId, QuadDatabaseCellFile[] sourceFiles)
        {
            CellReference = SkySegmentSphere.GetCellById(cellId);
            CellId = cellId;
            _sourceFiles = sourceFiles;
            Initialize();
        }


        private void Initialize()
        {
            // Gather pass quad densities from each file to a flat list for easier queries.

            var cellFilePassDensities = new List<CellFilePassDensity>();

            for (var i = 0; i < _sourceFiles.Length; i++)
                for (var p = 0; p < _sourceFiles[i].Descriptor.Passes.Length; p++)
                    cellFilePassDensities.Add(new CellFilePassDensity
                    {
                        FileIndex = i,
                        PassIndex = p,
                        QuadsPerSqDeg = _sourceFiles[i].Descriptor.Passes[p].QuadsPerSqDeg
                    });

            // Order by density, faster to access adjacent densities.
            _cellFilePassDensities = cellFilePassDensities
                .OrderBy(x => x.QuadsPerSqDeg)
                .ToArray();
        }
        
        
        /// <summary>
        /// Retrieves stars that are within specified angular distance.
        /// </summary>
        /// <param name="center">The center point in RA, Dec</param>
        /// <param name="angularDistance">Angular distance in degrees</param>
        /// <param name="quadsPerSqDegree">Image quad density</param>
        /// <param name="passOffset">If negative, use this offset to return quads from a more sparse quad density pass. If positive, return quads from a more dense quad density pass.
        /// Returns null if there are no more sparse/dense passes available.</param>
        /// <param name="sampling">If > 1, used to limit taken quads to 1/[sampling], so less quads taken.</param>
        /// <param name="imageQuads">The quads detected from the image.</param>
        /// <returns>StarQuad list, or null if passOffset was too big/small</returns>
        public StarQuad[] GetQuadsWithinRange(EquatorialCoords center, double angularDistance, int quadsPerSqDegree,
            int passOffset = 0, int numSubSets = 1, int subSetIndex = 0, ImageStarQuad[] imageQuads = null)
        {

            if (passOffset > _cellFilePassDensities.Length || passOffset < -_cellFilePassDensities.Length)
                return null;


            float closestDensityDiff = float.MaxValue;
            int bestDensityIndex = 0;

            for (var i = 0; i < _cellFilePassDensities.Length; i++)
            {
                var d = Math.Abs(_cellFilePassDensities[i].QuadsPerSqDeg - quadsPerSqDegree);
                if (d < closestDensityDiff)
                {
                    bestDensityIndex = i;
                    closestDensityDiff = d;
                }
            }

            var chosenIndex = bestDensityIndex;
            chosenIndex = bestDensityIndex + passOffset;

            if (passOffset < 0 && chosenIndex < 0)
                return null;

            if (passOffset > 0 && chosenIndex >= _cellFilePassDensities.Length)
                return null;
            

            var chosenPassDensity = _cellFilePassDensities[chosenIndex];
            return _sourceFiles[chosenPassDensity.FileIndex]
                .GetQuads(center, angularDistance, chosenPassDensity.PassIndex, numSubSets, subSetIndex, imageQuads);
            

        }

        public void Dispose()
        {
            if (!_disposing)
            {
                _disposing = true;
                if (_sourceFiles != null && _sourceFiles.Length > 0)
                {
                    foreach (var cellFile in _sourceFiles)
                    {
                        cellFile.Dispose();
                    }
                }
            }
        }
    }
}