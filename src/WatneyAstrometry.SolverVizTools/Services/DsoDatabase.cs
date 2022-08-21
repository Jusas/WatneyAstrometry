// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Models;
using WatneyAstrometry.SolverVizTools.Utils;
using FileStream = System.IO.FileStream;

namespace WatneyAstrometry.SolverVizTools.Services
{
    public class DsoDatabase : IDsoDatabase
    {

        private class RaSorter : IComparer<DeepSkyObject>
        {
            public int Compare(DeepSkyObject x, DeepSkyObject y)
            {
                return x.Coords.Ra < y.Coords.Ra
                    ? -1
                    : x.Coords.Ra > y.Coords.Ra
                        ? 1
                        : 0;
            }
        }

        private class DecSorter : IComparer<DeepSkyObject>
        {
            public int Compare(DeepSkyObject x, DeepSkyObject y)
            {
                return x.Coords.Dec < y.Coords.Dec
                    ? -1
                    : x.Coords.Dec > y.Coords.Dec
                        ? 1
                        : 0;
            }
        }

        private SortedSet<DeepSkyObject> _deepSkyObjectsByRa = new(new RaSorter());
        //private SortedSet<DeepSkyObject> _deepSkyObjectsByDec = new(new DecSorter());

        public bool IsLoaded => _deepSkyObjectsByRa.Count > 0;

        public bool HasDatabaseFileDownloaded => File.Exists(DatabaseFilePath);

        private string DatabaseFilePath = Path.Combine(ProgramEnvironment.ApplicationDataFolder, "dso.csv");

        private const string DsoDatabaseUrl = "https://raw.githubusercontent.com/astronexus/HYG-Database/master/dso.csv";

        public async Task Load(string filename = null)
        {
            _deepSkyObjectsByRa.Clear();
            //_deepSkyObjectsByDec.Clear();

            if (filename == null)
                filename = DatabaseFilePath;

            using var reader = new StreamReader(filename);
            string line;
            bool firstLine = true;
            
            // CSV, with a header line.
            // ra,dec,type,const,mag,name,rarad,decrad,id,r1,r2,angle,dso_source,id1,cat1,id2,cat2,dupid,dupcat,display_mag

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (firstLine)
                {
                    firstLine = false;
                    continue;
                }

                if (line.Length == 0)
                    continue;

                var dso = DeepSkyObject.FromCsvLine(line);
                _deepSkyObjectsByRa.Add(dso);
                //_deepSkyObjectsByDec.Add(dso);
            }
        }

        public List<DeepSkyObject> GetInRadius(double ra, double dec, double radius)
        {
            // Lazy implementation. Does not work well on polar regions.

            var objectsInView = new List<DeepSkyObject>();
            var center = new EquatorialCoords(ra, dec);

            (double, double)? raRange1 = null;
            (double, double)? raRange2 = null;

            var ra1 = ra - radius;
            var ra2 = ra + radius;
            if (ra1 < 0)
            {
                raRange1 = (ra1 + 360, 360);
                raRange2 = (0, ra2 % 360);
            }
            else if(ra2 > 360)
            {
                raRange1 = (ra1, 360);
                raRange2 = (0, ra2 % 360);
            }
            else
            {
                raRange1 = (ra1, ra2);
            }
            
            var raFiltered = _deepSkyObjectsByRa.GetViewBetween(DeepSkyObject.ComparisonObject(raRange1.Value.Item1, 0),
                DeepSkyObject.ComparisonObject(raRange1.Value.Item2, 0));

            foreach (var dso in raFiltered)
            {
                if (dso.Coords.GetAngularDistanceTo(center) <= radius)
                    objectsInView.Add(dso);
            }

            if (raRange2 != null)
            {
                var raFiltered2 = _deepSkyObjectsByRa.GetViewBetween(DeepSkyObject.ComparisonObject(raRange2.Value.Item1, 0),
                    DeepSkyObject.ComparisonObject(raRange2.Value.Item2, 0));

                foreach (var dso in raFiltered2)
                {
                    if (dso.Coords.GetAngularDistanceTo(center) <= radius)
                        objectsInView.Add(dso);
                }
            }
            

            return objectsInView;

        }

        public async Task<string> DownloadDatabase(Action<double> progressCallback, CancellationToken cancellationToken)
        {
            //using var httpClient = new HttpClient();

            //using var response = await httpClient.GetAsync(DsoDatabaseUrl, HttpCompletionOption.ResponseHeadersRead);
            //var contentLength = response.Content.Headers.ContentLength;
            //Directory.CreateDirectory(Path.GetDirectoryName(DatabaseFilePath));

            //using FileStream fileStream = new FileStream(DatabaseFilePath, FileMode.Create);
        
            //using var download = await response.Content.ReadAsStreamAsync();

            //if (!contentLength.HasValue)
            //{
            //    progressCallback(0.0);
            //    Task t = download.CopyToAsync(fileStream, cancellationToken);
            //    await t;
            //    if (t.IsCanceled)
            //    {
            //        if(File.Exists(DatabaseFilePath))
            //            File.Delete(DatabaseFilePath);

            //        return null;
            //    }

            //    progressCallback(100.0);
            //}
            //else
            //{
            //    var buf = new byte[81920];
            //    var totalBytesRead = 0l;
            //    var bytesRead = 0;

            //    while ((bytesRead = await download.ReadAsync(buf, 0, buf.Length, cancellationToken)) > 0)
            //    {
            //        await fileStream.WriteAsync(buf, 0, bytesRead, cancellationToken);
            //        totalBytesRead += bytesRead;
            //        progressCallback((double)totalBytesRead / contentLength.Value * 100.0);
            //    }
            //}

            //return DatabaseFilePath;

            var downloadedFile = await DownloadUtils.DownloadWithProgressReporting(DsoDatabaseUrl, ProgramEnvironment.ApplicationDataFolder,
                "dso.csv", true, (percent, bytes) => progressCallback(percent), cancellationToken);

            return downloadedFile.FullName;
        }
    }
}
