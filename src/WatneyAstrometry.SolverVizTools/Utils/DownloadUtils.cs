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
using WatneyAstrometry.SolverVizTools.Exceptions;

namespace WatneyAstrometry.SolverVizTools.Utils
{
    public static class DownloadUtils
    {

        public static async Task<FileInfo> DownloadWithProgressReporting(string url, string directory, string filename, bool overwrite,
            Action<double /* percent */, long /* bytes */> progressCallback, CancellationToken cancellationToken)
        {
            var fullFilePath = Path.Combine(directory, filename);

            try
            {
                using var httpClient = new HttpClient();

                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                var contentLength = response.Content.Headers.ContentLength;
                Directory.CreateDirectory(directory);
                
                if (!overwrite && File.Exists(fullFilePath))
                    throw new IOException($"File {fullFilePath} already exists and not allowed to overwrite");

                using FileStream fileStream = new FileStream(fullFilePath, FileMode.Create);

                using var download = await response.Content.ReadAsStreamAsync();

                if (!contentLength.HasValue)
                {
                    progressCallback(0.0, 0);
                    Task t = download.CopyToAsync(fileStream, cancellationToken);
                    await t;
                    if (t.IsCanceled)
                    {
                        if (File.Exists(fullFilePath))
                            File.Delete(fullFilePath);

                        return null;
                    }

                    progressCallback(100.0, fileStream.Length);
                }
                else
                {
                    var buf = new byte[81920];
                    var totalBytesRead = 0l;
                    var bytesRead = 0;

                    while ((bytesRead = await download.ReadAsync(buf, 0, buf.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buf, 0, bytesRead, cancellationToken);
                        totalBytesRead += bytesRead;
                        progressCallback((double)totalBytesRead / contentLength.Value * 100.0, totalBytesRead);
                    }
                }

                return new FileInfo(fullFilePath);
            }
            catch (TaskCanceledException e)
            {
                if (File.Exists(fullFilePath))
                    File.Delete(fullFilePath);

                progressCallback(0, 0);
                return null;
            }
            catch (Exception e)
            {
                throw new DownloadException($"File download failed: {e.Message}", e);
            }
        }
    }
}
