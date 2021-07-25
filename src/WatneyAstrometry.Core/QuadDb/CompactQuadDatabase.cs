// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core.QuadDb
{
    /// <summary>
    /// A file based star quad database, reading the files built with the GaiaQuadDatabaseCreator.
    /// </summary>
    public class CompactQuadDatabase : IQuadDatabase, IDisposable
    {
        internal QuadDatabaseCellFileSet[] _cellFileSets;
        private bool _disposing;
        
        public CompactQuadDatabase()
        {
        }

        /// <summary>
        /// Uses the data files found from a directory.
        /// The database is a set of files with the .qdb extension.
        /// </summary>
        /// <param name="directoryPath">The directory where the .qdb files are located.</param>
        /// <param name="loadIntoMemory">Loads the whole data set into memory for faster access times. <br/>
        /// If false, the files are accessed as needed for a much smaller memory footprint. <br/>
        /// If true, the files are loaded into memory resulting in faster access times.
        /// <br/>Defaults to false.</param>
        public CompactQuadDatabase UseDataSource(string directoryPath, bool loadIntoMemory = false)
        {

            if (!Directory.Exists(directoryPath))
                throw new Exception($"Directory {directoryPath} does not exist, unable to load/use star database");
            
            var dbCellFiles = Directory.GetFiles(directoryPath, "*.qdb");
            
            _cellFileSets = QuadDatabaseCellFileSet.ReadDatabaseCellFiles(dbCellFiles);
            
            if (loadIntoMemory)
            {
                Parallel.ForEach(_cellFileSets, file =>
                {
                    file.LoadIntoMemory();
                });
            }

            return this;
        }

        /// <summary>
        /// Gets the quads around the given center point, within the given radius.
        /// <br/>
        /// The database contains passes, and the pass chosen for use is determined by the <see cref="quadsPerSqDegree"/> parameter. Using <see cref="quadDensityOffsets"/>
        /// you can control the passes included in the results.
        /// <br/>
        /// <see cref="imageQuads"/> is used to filter out quads that do not match, so only the quads that could potentially match are included in the results.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radiusDegrees">The radius around the center.</param>
        /// <param name="quadsPerSqDegree">The quads per square degree, calculated from the image and assumed field size.</param>
        /// <param name="quadDensityOffsets">Offsets used to include passes with lower or higher quad density. Example: [-1, 0, 1]. Can be null, which will equal to [0].</param>
        /// <param name="imageQuads">The quads formed from the source image's stars.</param>
        /// <returns></returns>
        public async Task<List<StarQuad>> GetQuadsAsync(EquatorialCoords center, double radiusDegrees, int quadsPerSqDegree, int[] quadDensityOffsets, ImageStarQuad[] imageQuads)
        {
            var cells = SkySegmentSphere.Cells;
            var cellsToInclude = new string[cells.Count];
            int cellsToIncludeCount = 0;
            for (int i = 0; i < cellsToInclude.Length; i++)
            {
                var cell = cells[i];
                if (BandsAndCells.IsCellInSearchRadius(radiusDegrees, center, cell.Bounds)) // optimize
                {
                    cellsToInclude[cellsToIncludeCount] = cell.CellId;
                    cellsToIncludeCount++;
                }
            }
            
            var sourceDataFileSets = new List<QuadDatabaseCellFileSet>(cellsToIncludeCount);
            for (var i = 0; i < _cellFileSets.Length; i++)
            {
                for (var j = 0; j < cellsToIncludeCount; j++)
                {
                    if (_cellFileSets[i].CellId == cellsToInclude[j])
                    {
                        sourceDataFileSets.Add(_cellFileSets[i]);
                        break;
                    }
                }
            }
            
            if (quadDensityOffsets == null || quadDensityOffsets.Length == 0)
                quadDensityOffsets = new int[] {0};


            List<Task> tasks = new List<Task>();
            
            var quadListByDensity = new StarQuad[quadDensityOffsets.Length][][];

            for (var i = 0; i < quadDensityOffsets.Length; i++)
                quadListByDensity[i] = new StarQuad[sourceDataFileSets.Count][];


            for (var i = 0; i < quadDensityOffsets.Length; i++)
            {
                for (var s = 0; s < sourceDataFileSets.Count; s++)
                {
                    var source = s;
                    var offset = quadDensityOffsets[i];
                    var idx = i;
                    tasks.Add(Task.Run(() =>
                    {
                        quadListByDensity[idx][source] = sourceDataFileSets[source].GetQuadsWithinRange(center, radiusDegrees, quadsPerSqDegree, offset, imageQuads);
                    }));

                }
            }

            await Task.WhenAll(tasks);
            return quadListByDensity
                .SelectMany(x => x ?? new StarQuad[0][])
                .SelectMany(x => x ?? new StarQuad[0])
                .ToArray()
                .Distinct(new StarQuad.StarQuadRatioBasedEqualityComparer())
                .ToList();
            
        }


        public void Dispose()
        {
            if (!_disposing)
            {
                _disposing = true;
                if (_cellFileSets != null && _cellFileSets.Length > 0)
                {
                    foreach (var cellFileSet in _cellFileSets)
                    {
                        cellFileSet.Dispose();
                    }
                }
            }
            
        }
    }
}