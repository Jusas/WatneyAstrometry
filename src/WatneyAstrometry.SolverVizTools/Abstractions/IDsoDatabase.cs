using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WatneyAstrometry.SolverVizTools.Models;

namespace WatneyAstrometry.SolverVizTools.Abstractions;

public interface IDsoDatabase
{
    bool IsLoaded { get; }
    bool HasDatabaseFileDownloaded { get; }
    Task Load(string filename = null);
    List<DeepSkyObject> GetInRadius(double ra, double dec, double radius);
    Task<string> DownloadDatabase(Action<double> progressCallback, CancellationToken cancellationToken);
}