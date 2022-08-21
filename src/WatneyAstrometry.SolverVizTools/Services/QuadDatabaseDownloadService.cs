// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Exceptions;
using WatneyAstrometry.SolverVizTools.Models;
using WatneyAstrometry.SolverVizTools.Utils;

namespace WatneyAstrometry.SolverVizTools.Services
{
    public class QuadDatabaseDownloadService : IQuadDatabaseDownloadService
    {
        private string _databaseDir;
        public string DatabaseDir => _databaseDir;
        private const string BaseUrl = "https://github.com/Jusas/WatneyAstrometry/releases/download/watneyqdb3";
        private QuadDatabaseDataSet[] _quadDatabaseDataSets = new QuadDatabaseDataSet[]
        {
            new QuadDatabaseDataSet($"{BaseUrl}/watneyqdb-00-07-20-v3.zip", 0, 7, 20,
                "352 MB", "Passes 0..7 (>= 0.8 deg field radius)"),
            new QuadDatabaseDataSet($"{BaseUrl}/watneyqdb-08-09-20-v3.zip", 8, 9, 20,
                "372 MB", "Passes 8..9 (0.7 .. 0.6 deg field radius)"),
            new QuadDatabaseDataSet($"{BaseUrl}/watneyqdb-10-11-20-v3.zip", 10, 11, 20,
                "744 MB", "Passes 10..11 (0.5 .. 0.4 deg field radius)"),
            new QuadDatabaseDataSet($"{BaseUrl}/watneyqdb-12-13-20-v3.zip", 12, 13, 20,
                "1.45 GB", "Passes 12..13 (0.3 deg field radius)"),
            new QuadDatabaseDataSet($"{BaseUrl}/watneyqdb-14-20-v3.zip", 14, 14, 20,
                "1.2 GB", "Pass 14 (0.2 deg field radius)")
        };

        public QuadDatabaseDataSet[] DownloadableQuadDatabaseDataSets => _quadDatabaseDataSets;

        public QuadDatabaseDownloadService()
        {
            //_quadDatabaseDataSets[0].IsDownloaded = true;
            //_quadDatabaseDataSets[1].DownloadProgress = 0.5;
        }

        public void SetDatabaseDirectory(string directory)
        {
            _databaseDir = directory;
            foreach (var quadDatabaseDataSet in _quadDatabaseDataSets)
                quadDatabaseDataSet.CheckAndUpdateIsDownloaded(_databaseDir);
        }

        public async Task DownloadDataSet(QuadDatabaseDataSet dataSet, 
            CancellationToken cancellationToken)
        {
            var filename = $"__db_download_temp_{Guid.NewGuid().ToString()}.zip";
            var fullFilePath = Path.Combine(_databaseDir, filename);

            try
            {
                var downloadedFile = await DownloadUtils.DownloadWithProgressReporting(dataSet.Url, _databaseDir,
                    filename, true,
                    (percent, bytes) => { Dispatcher.UIThread.Post(() => dataSet.DownloadProgress = percent); },
                    cancellationToken);

                if (downloadedFile == null)
                {
                    dataSet.DownloadProgress = 0;
                    dataSet.CheckAndUpdateIsDownloaded(_databaseDir);

                    if (File.Exists(fullFilePath))
                        File.Delete(fullFilePath);

                    return;
                }

                using (var fileStream = new FileStream(downloadedFile.FullName, FileMode.Open, FileAccess.Read))
                {
                    using var zip = new ZipArchive(fileStream);

                    await Task.Run(() => zip.ExtractToDirectory(_databaseDir, true));
                }

                if (File.Exists(fullFilePath))
                    File.Delete(fullFilePath);

                dataSet.CheckAndUpdateIsDownloaded(_databaseDir);
            }
            catch (Exception e)
            {
                if (File.Exists(fullFilePath))
                    File.Delete(fullFilePath);

                dataSet.DownloadProgress = 0;
                dataSet.CheckAndUpdateIsDownloaded(_databaseDir);
                throw;
            }
            
        }


    }
}
