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


        private QuadDatabaseCellFileSet(string cellId)
        {
            CellReference = SkySegmentSphere.GetCellById(cellId);
            CellId = cellId;
        }
        

        /// <summary>
        /// Read the files (*.qdb) into cell file sets (a set of files per cell).
        /// </summary>
        /// <param name="filenames"></param>
        /// <returns></returns>
        public static QuadDatabaseCellFileSet[] ReadDatabaseCellFiles(string[] filenames)
        {
            var cellFileSets = new Dictionary<string, QuadDatabaseCellFileSet>();

            for (var i = 0; i < filenames.Length; i++)
            {
                var filename = filenames[i];
                var cellFile = new QuadDatabaseCellFile(filename);
                var cellFileSet = cellFileSets.ContainsKey(cellFile.FileDescriptor.CellId) 
                    ? cellFileSets[cellFile.FileDescriptor.CellId] 
                    : new QuadDatabaseCellFileSet(cellFile.FileDescriptor.CellId);
                cellFileSet.AddCellFile(cellFile);
                cellFileSets[cellFile.FileDescriptor.CellId] = cellFileSet;
            }

            var sets = cellFileSets.Values.ToArray();

            for (var i = 0; i < sets.Length; i++)
                sets[i].Initialize();

            return cellFileSets.Values.ToArray();
        }
        

        private void Initialize()
        {
            // Gather pass quad densities from each file to a flat list for easier queries.

            var cellFilePassDensities = new List<CellFilePassDensity>();

            for (var i = 0; i < _sourceFiles.Length; i++)
                for (var p = 0; p < _sourceFiles[i].FileDescriptor.Passes.Length; p++)
                    cellFilePassDensities.Add(new CellFilePassDensity
                    {
                        FileIndex = i,
                        PassIndex = p,
                        QuadsPerSqDeg = _sourceFiles[i].FileDescriptor.Passes[p].QuadsPerSqDeg
                    });

            // Order by density, faster to access adjacent densities.
            _cellFilePassDensities = cellFilePassDensities
                .OrderBy(x => x.QuadsPerSqDeg)
                .ToArray();
        }

        private void AddCellFile(QuadDatabaseCellFile cellFile)
        {
            _sourceFiles = _sourceFiles.Append(cellFile).ToArray();
        }
        

        /// <summary>
        /// Loads all stars in the files into memory.
        /// </summary>
        public void LoadIntoMemory()
        {
            throw new NotImplementedException();
            //_cachedQuads = new Dictionary<string, Dictionary<int, StarQuad[]>>(_sourceFiles.Length);
            //foreach (var fn in _sourceFiles)
            //    _cachedQuads[fn.Filename] = new Dictionary<int, StarQuad[]>();

            //var singleQuadDataLen = /*ratios*/ sizeof(float) * 5 + /*largestDist*/ sizeof(float) + /*coords*/ sizeof(float) * 2;


            //Parallel.ForEach(_sourceFiles, delegate(QuadCellFile file, ParallelLoopState state)
            //{
            //    using (var stream = new FileStream(file.Filename, FileMode.Open, FileAccess.Read))
            //    {
            //        var cache = _cachedQuads[file.Filename];
            //        for (var i = 0; i < file.SubCellInfos.Length; i++)
            //        {
            //            var subCellInfo = file.SubCellInfos[i];
            //            stream.Seek(subCellInfo.DataStartPosition, SeekOrigin.Begin);
            //            var buf = new byte[subCellInfo.DataLength];
            //            stream.Read(buf, 0, buf.Length);

            //            var quadCount = subCellInfo.DataLength / singleQuadDataLen;
            //            var quads = new List<StarQuad>(quadCount);
            //            int advance = 0;
            //            for (var j = 0; j < quadCount; j++)
            //            {
            //                var quad = BytesToQuad(buf, advance);
            //                quads.Add(quad);
            //                advance += singleQuadDataLen;
            //            }

            //            cache.Add(i, quads.ToArray());
            //        }
            //    }
            //});

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
            int passOffset = 0, int sampling = 0, ImageStarQuad[] imageQuads = null)
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
                .GetQuads(center, angularDistance, chosenPassDensity.PassIndex, sampling, imageQuads);
            

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