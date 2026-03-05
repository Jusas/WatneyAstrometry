namespace ConsoleSolverBenchmarkTool.Services;

using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using ConsoleSolverBenchmarkTool.Config;
using ConsoleSolverBenchmarkTool.Models;

public class Downloader
{
    private readonly HttpClient _httpClient = new();
    private readonly string _currentArchitecture;

    public Downloader(string currentArchitecture)
    {
        _currentArchitecture = currentArchitecture;
    }

    public async Task DownloadAllAsync(BenchmarkEnvironmentStatus status)
    {
        try
        {
            Console.WriteLine("Downloading solvers");
            Console.WriteLine();
            foreach (var solver in status.Solvers.Where(s => !s.IsInstalled && s.IsDownloadable))
                await DownloadSolverAsync(solver.Config);

            Console.WriteLine("Downloading databases");
            Console.WriteLine();
            foreach (var db in status.Databases.Where(d => !d.IsInstalled && d.IsDownloadable))
                await DownloadDatabaseAsync(db.Config);

            Console.WriteLine("Downloading datasets");
            Console.WriteLine();
            foreach (var ds in status.Datasets.Where(d => !d.IsInstalled && d.IsDownloadable))
                await DownloadDatasetAsync(ds.Config);

            Console.WriteLine("Done!");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error downloading assets: {ex.Message}");
            throw new Exception("Failed to download all assets", ex);
        }
    }

    public async Task DownloadSolverAsync(SolverConfig config)
    {
        try
        {
            var url = config.GetDownloadUrl(_currentArchitecture);
            if (url == null)
            {
                Console.WriteLine($"- {config.Name}: no download available for architecture '{_currentArchitecture}'");
                return;
            }

            Console.WriteLine($"- {config.Name}");
            Console.WriteLine($"  - Target directory: {config.Directory}");

            Directory.CreateDirectory(config.Directory);
            var fileName = GetFileName(url);
            var tempFile = Path.Combine(Path.GetTempPath(), fileName);

            Console.WriteLine($"  - Downloading {url} ...");
            await DownloadFileToPathAsync(url, tempFile);

            Console.WriteLine($"  - Extracting {fileName} ...");
            await ExtractAsync(tempFile, config.Directory, url);
            File.Delete(tempFile);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error downloading solver '{config.Name}': {ex.Message}");
            throw new Exception($"Failed to download solver '{config.Name}'", ex);
        }
    }

    public async Task DownloadDatabaseAsync(DatabaseConfig config)
    {
        try
        {
            if (!config.HasAnyDownloads)
            {
                Console.WriteLine($"- {config.Name}: no downloads configured");
                return;
            }

            Console.WriteLine($"- {config.Name}");
            Console.WriteLine($"  - Target directory: {config.Directory}");
            Directory.CreateDirectory(config.Directory);

            var tempFiles = new List<(string TempPath, string Url)>();
            foreach (var url in config.Downloads!)
            {
                Console.WriteLine($"  - Downloading {url} ...");
                var fileName = GetFileName(url);
                var tempFile = Path.Combine(Path.GetTempPath(), fileName);
                await DownloadFileToPathAsync(url, tempFile);
                tempFiles.Add((tempFile, url));
            }

            foreach (var (tempFile, url) in tempFiles)
            {
                Console.WriteLine($"  - Extracting {Path.GetFileName(tempFile)} ...");
                await ExtractAsync(tempFile, config.Directory, url);
                File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error downloading database '{config.Name}': {ex.Message}");
            throw new Exception($"Failed to download database '{config.Name}'", ex);
        }
    }

    public async Task DownloadDatasetAsync(DatasetConfig config)
    {
        try
        {
            if (!config.HasAnyDownloads)
            {
                Console.WriteLine($"- {config.Name}: no downloads configured");
                return;
            }

            Console.WriteLine($"- {config.Name}");
            Console.WriteLine($"  - Target directory: {config.Directory}");
            Directory.CreateDirectory(config.Directory);

            var tempFiles = new List<(string TempPath, string Url)>();
            foreach (var url in config.Downloads!)
            {
                Console.WriteLine($"  - Downloading {url} ...");
                var fileName = GetFileName(url);
                var tempFile = Path.Combine(Path.GetTempPath(), fileName);
                await DownloadFileToPathAsync(url, tempFile);
                tempFiles.Add((tempFile, url));
            }

            foreach (var (tempFile, url) in tempFiles)
            {
                Console.WriteLine($"  - Extracting {Path.GetFileName(tempFile)} ...");
                await ExtractAsync(tempFile, config.Directory, url);
                File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error downloading dataset '{config.Name}': {ex.Message}");
            throw new Exception($"Failed to download dataset '{config.Name}'", ex);
        }
    }

    private async Task DownloadFileToPathAsync(string url, string destPath)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var fileStream = File.Create(destPath);
        await response.Content.CopyToAsync(fileStream);
    }

    private static async Task ExtractAsync(string archivePath, string targetDirectory, string originalUrl)
    {
        if (originalUrl.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase) ||
            originalUrl.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-xzf \"{archivePath}\" -C \"{targetDirectory}\"",
                    UseShellExecute = false,
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
                throw new Exception($"tar exited with code {process.ExitCode} while extracting '{archivePath}'");
        }
        else
        {
            ZipFile.ExtractToDirectory(archivePath, targetDirectory, overwriteFiles: true);
        }
    }

    private static string GetFileName(string url) =>
        Path.GetFileName(new Uri(url).LocalPath);
}
