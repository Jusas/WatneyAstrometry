// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Exceptions;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.Threading;
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

        private ConcurrentDictionary<Guid, QuadDatabaseSolveInstanceMemoryCache> _contexts =
            new ConcurrentDictionary<Guid, QuadDatabaseSolveInstanceMemoryCache>();

        public string DatabaseDirectory { get; private set; }

        /// <summary>
        /// New instance of quad database.
        /// </summary>
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
                throw new QuadDatabaseException($"Directory {directoryPath} does not exist, unable to load/use star database");

            DatabaseDirectory = directoryPath;

            var indexes = QuadDatabaseCellFileIndex.ReadAllIndexes(directoryPath);
            _cellFileSets = QuadDatabaseCellFileSet.FromIndexes(indexes);
            
            if (loadIntoMemory)
            {
                throw new NotImplementedException("Loading to memory not supported yet");
            }

            return this;
        }

        /// <summary>
        /// Gets the quads around the given center point, within the given radius.
        /// <br/>
        /// The database contains passes, and the pass chosen for use is determined by the <paramref name="quadsPerSqDegree"/> parameter. Using <paramref name="quadDensityOffsets"/>
        /// you can control the passes included in the results.
        /// <br/>
        /// <paramref name="sortedImageQuads"/> is used to filter out quads that do not match, so only the quads that could potentially match are included in the results.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="radiusDegrees">The radius around the center.</param>
        /// <param name="quadsPerSqDegree">The quads per square degree, calculated from the image and assumed field size.</param>
        /// <param name="quadDensityOffsets">Offsets used to include passes with lower or higher quad density. Example: [-1, 0, 1]. Can be null, which will equal to [0].</param>
        /// <param name="subSetIndex">Index of subset. Sampling divides database quads to subsets.</param>
        /// <param name="numSubSets">Number of subsets (i.e. sampling)</param>
        /// <param name="sortedImageQuads">The quads formed from the source image's stars, sorted by the first ratio (descending).</param>
        /// <param name="solveContextId">Which solve context we're working on. Contexts are used for caching to speed up the process.</param>
        /// <returns></returns>
        public List<StarQuad> GetQuads(EquatorialCoords center, double radiusDegrees, int quadsPerSqDegree, 
            int[] quadDensityOffsets, int numSubSets, int subSetIndex, ImageStarQuad[] sortedImageQuads, Guid solveContextId)
        {
            if (!_contexts.TryGetValue(solveContextId, out var cache))
                throw new QuadDatabaseException($"Context {solveContextId} doesn't exist, it should be created first");

            var cells = SkySegmentSphere.Cells;

            int[] cellsToInclude = null;
            int cellsToIncludeCount = 0;
            
            // Caching this, since otherwise there would be so many calls to IsCellInSearchRadius.
            // The cache helps when using sampling, as we potentially need to go over the same areas multiple times
            // just with a different set of DB quads to check.
            // Using the (RA, Dec) and radius as keys, and using rounding/casting to int to make "clever" keys without
            // having to use floating point comparison.
            
            var radiusRounded = (int)Math.Max(1, radiusDegrees * 100);
            var centerRaRounded = (int)(center.Ra * 100_000);
            var centerDecRounded = (int)(center.Dec * 100_000);
            // Two ints as one long makes for the key.
            var cellSearchCacheKey = ((long)centerRaRounded << 32) | (centerDecRounded & 0xffffffffL);

            // TODO: When sampling == 1, skip this caching since it gives us no advantage as we only run each area once.
            if (cache.CellSearchCache.TryGetValue(cellSearchCacheKey, out var cellSearchRaDecCacheEntry) &&
                cellSearchRaDecCacheEntry.TryGetValue(radiusRounded, out var radiusCacheEntry))
            {
                cellsToInclude = radiusCacheEntry;
                cellsToIncludeCount = radiusCacheEntry.Length;
            }
            else
            {
                cellsToInclude = new int[cells.Count];
                
                for (int i = 0; i < cellsToInclude.Length; i++)
                {
                    var cell = cells[i];
                    if (BandsAndCells.IsCellInSearchRadius(radiusDegrees, center, cell.Bounds)) 
                    {
                        cellsToInclude[cellsToIncludeCount] = cell.CellIdNumber;
                        cellsToIncludeCount++;
                    }
                }
                Array.Resize(ref cellsToInclude, cellsToIncludeCount);
                if (cellSearchRaDecCacheEntry == null)
                {
                    var dictionary = new ConcurrentDictionary<int, int[]>();
                    dictionary.TryAdd(radiusRounded, cellsToInclude);
                    cache.CellSearchCache.TryAdd(cellSearchCacheKey, dictionary);
                }
                else
                {
                    cellSearchRaDecCacheEntry.TryAdd(radiusRounded, cellsToInclude);
                }
            }
            
            
            var sourceDataFileSets = new List<QuadDatabaseCellFileSet>(cellsToIncludeCount);
            for (var i = 0; i < _cellFileSets.Length; i++)
            {
                for (var j = 0; j < cellsToIncludeCount; j++)
                {
                    if (_cellFileSets[i].CellIdNumber == cellsToInclude[j])
                    {
                        sourceDataFileSets.Add(_cellFileSets[i]);
                        break;
                    }
                }
            }
            
            if (quadDensityOffsets == null || quadDensityOffsets.Length == 0)
                quadDensityOffsets = new int[] {0};
            
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

                    quadListByDensity[idx][source] = sourceDataFileSets[source].GetQuadsWithinRange(
                        center, radiusDegrees, quadsPerSqDegree, offset, numSubSets, subSetIndex, sortedImageQuads, cache);

                }
            }
            
            return quadListByDensity
                .SelectMany(x => x ?? new StarQuad[0][])
                .SelectMany(x => x ?? new StarQuad[0])
                .ToArray()
                .Distinct(new StarQuad.StarQuadRatioBasedEqualityComparer())
                .ToList();
            
        }


        /// <summary>
        /// Create a new solve context. Contexts are used for caching to speed up the quad lookups.
        /// </summary>
        /// <param name="contextId"></param>
        /// <exception cref="QuadDatabaseException"></exception>
        public void CreateSolveContext(Guid contextId)
        {
            if (!_contexts.TryAdd(contextId, CreateMemoryCacheObject()))
                throw new QuadDatabaseException($"Solve context {contextId} already exists");
        }

        /// <summary>
        /// Disposes a solve context.
        /// </summary>
        /// <param name="contextId"></param>
        public void DisposeSolveContext(Guid contextId)
        {
            _contexts.TryRemove(contextId, out var _);
        }

        private QuadDatabaseSolveInstanceMemoryCache CreateMemoryCacheObject()
        {
            var filesTotal = _cellFileSets.Sum(x => x.SourceFiles.Count);
            var cacheObject = new QuadDatabaseSolveInstanceMemoryCache();
            var cacheFileEntries = new QuadDatabaseSolveInstanceMemoryCache.FileCachedData[filesTotal];
            
            foreach (var cellFileSet in _cellFileSets)
            {
                foreach (var file in cellFileSet.SourceFiles)
                {
                    var fileEntry = new QuadDatabaseSolveInstanceMemoryCache.FileCachedData();
                    cacheFileEntries[file.FileId] = fileEntry;
                    fileEntry.Passes = new QuadDatabaseSolveInstanceMemoryCache.PassCachedData[file.Descriptor.Passes.Length];

                    for(var p = 0; p < file.Descriptor.Passes.Length; p++)
                    {
                        var passEntry = new QuadDatabaseSolveInstanceMemoryCache.PassCachedData();
                        fileEntry.Passes[p] = passEntry;
                        passEntry.SubCells =
                            new QuadDatabaseSolveInstanceMemoryCache.SubCellCachedData[file.Descriptor.Passes[p]
                                .SubCells.Length];
                        for (var sc = 0; sc < passEntry.SubCells.Length; sc++)
                        {
                            passEntry.SubCells[sc] = new QuadDatabaseSolveInstanceMemoryCache.SubCellCachedData();
                        }
                    }
                }
            }

            cacheObject.Files = cacheFileEntries;
            return cacheObject;

        }

        /// <summary>
        /// Disposes the resources it and its children have reserved.
        /// </summary>
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