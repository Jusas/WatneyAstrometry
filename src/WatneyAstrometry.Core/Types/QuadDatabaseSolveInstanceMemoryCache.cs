// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// A memory cache object, one per solve instance.
    /// </summary>
    internal class QuadDatabaseSolveInstanceMemoryCache
    {
        public class PassCachedData
        {
            public SubCellCachedData[] SubCells { get; set; }
        }

        public class SubCellCachedData
        {
            public StarQuad[][] QuadsForSubset { get; set; }
            public StarQuad[] QuadsFullSet { get; set; }
        }

        public class FileCachedData
        {
            public PassCachedData[] Passes { get; set; }
        }
        
        public FileCachedData[] Files { get; set; }

        public ConcurrentDictionary<long, ConcurrentDictionary<int, int[]>> CellSearchCache { get; } = new();

        public QuadDatabaseSolveInstanceMemoryCache()
        {
            
        }

    }
}