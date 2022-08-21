// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatneyAstrometry.SolverVizTools.Models;

namespace WatneyAstrometry.SolverVizTools.Abstractions
{
    public interface IQuadDatabaseDownloadService
    {
        string DatabaseDir { get; }
        QuadDatabaseDataSet[] DownloadableQuadDatabaseDataSets { get; }
        void SetDatabaseDirectory(string directory);
        Task DownloadDataSet(QuadDatabaseDataSet dataSet, 
            CancellationToken cancellationToken);
    }
}
