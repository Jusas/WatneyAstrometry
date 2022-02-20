// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

namespace WatneyAstrometry.Core.Types
{
    /// <summary>
    /// A memory cache object, one per solve instance.
    /// </summary>
    public class QuadDatabaseSolveInstanceMemoryCache
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

        public QuadDatabaseSolveInstanceMemoryCache()
        {
            
        }

    }
}