namespace ConsoleSolverBenchmarkTool.UI;

using ConsoleSolverBenchmarkTool.Models;
using ConsoleSolverBenchmarkTool.Services;

public class InteractionLoop
{
    private readonly string _configPath;
    private readonly ConfigLoader _configLoader;
    private readonly EnvironmentChecker _checker;
    private readonly Downloader _downloader;
    private readonly BenchmarkRunner _runner;

    public InteractionLoop(string configPath)
    {
        _configPath = configPath;
        _configLoader = new ConfigLoader();
        _checker = new EnvironmentChecker();
        _downloader = new Downloader(_checker.CurrentArchitecture);
        _runner = new BenchmarkRunner();
    }

    public async Task RunAsync()
    {
        while (true)
        {
            var config = _configLoader.Load(_configPath);
            var status = _checker.Check(config);

            PrintStatus(status);

            Console.WriteLine("Available actions:");
            Console.WriteLine("  1. Download all downloadable solvers and assets");
            Console.WriteLine("  2. Download selected asset");
            Console.WriteLine("  3. Run a benchmark");
            Console.WriteLine("  q. Exit");
            Console.WriteLine();
            Console.Write("Select action: ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    await _downloader.DownloadAllAsync(status);
                    break;
                case "2":
                    await HandleDownloadSelectedAsync(status);
                    break;
                case "3":
                    HandleRunBenchmark(status);
                    break;
                case "q":
                    return;
                default:
                    Console.WriteLine("Invalid selection. Please try again.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private static void PrintStatus(BenchmarkEnvironmentStatus status)
    {
        ConsolePrint.PrintBigSeparator();
        Console.WriteLine("                      WATNEY BENCHMARKING");
        ConsolePrint.PrintBigSeparator();
        Console.WriteLine();
        Console.WriteLine("Solvers:");
        for (int i = 0; i < status.Solvers.Count; i++)
        {
            var s = status.Solvers[i];
            var downloadability = s.IsDownloadable ? "[downloadable]" : "[local]";
            var installStatus = s.IsInstalled ? "INSTALLED" : "not detected as installed";
            
            Console.ForegroundColor = s.IsInstalled ? ConsoleColor.Green : ConsoleColor.DarkGray;
            Console.WriteLine($"- (s{i + 1}) {s.Config.Name}: {installStatus}, {downloadability}");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine("Quad Databases:");
        for (int i = 0; i < status.Databases.Count; i++)
        {
            var d = status.Databases[i];
            var downloadability = d.IsDownloadable ? "[downloadable]" : "[local]";
            var installStatus = d.IsInstalled ? "INSTALLED" : "not detected as installed";
            
            Console.ForegroundColor = d.IsInstalled ? ConsoleColor.Green : ConsoleColor.DarkGray;
            Console.WriteLine($"- (d{i + 1}) {d.Config.Name}: {installStatus}, {downloadability}");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine("Datasets:");
        for (int i = 0; i < status.Datasets.Count; i++)
        {
            var ds = status.Datasets[i];
            var downloadability = ds.IsDownloadable ? "[downloadable]" : "[local]";
            var installStatus = ds.IsInstalled ? "INSTALLED" : "not detected as installed";
            
            Console.ForegroundColor = ds.IsInstalled ? ConsoleColor.Green : ConsoleColor.DarkGray;
            Console.WriteLine($"- (ds{i + 1}) {ds.Config.Name}: {installStatus}, {downloadability}");
            Console.ResetColor();
        }
        
        Console.WriteLine();
        Console.WriteLine("Profiles:");
        for (int i = 0; i < status.Profiles.Count; i++)
        {
            var p = status.Profiles[i];
            Console.WriteLine($"- (p{i + 1}) {p.Config.Name}");
        }

        Console.WriteLine();
        ConsolePrint.PrintSmallSeparator();
        Console.WriteLine();
        
        var availableRuns = status.Runs.Where(r => r.IsAvailable).ToList();
        Console.WriteLine("Available runs:");
        Console.WriteLine();
        for (int i = 0; i < availableRuns.Count; i++)
        {
            var r = availableRuns[i];
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"- (r{i + 1}) {r.Config.Name}:");
            Console.ResetColor();
            Console.WriteLine($"  * profile: {r.Profile.Config.Name}");
            Console.WriteLine($"  * dataset: {r.Dataset.Config.Name}");
            Console.WriteLine();
        }

        Console.WriteLine();
    }

    private async Task HandleDownloadSelectedAsync(BenchmarkEnvironmentStatus status)
    {
        Console.Write("Select asset package to download (sN, dN, pN, dsN, rN): ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        Console.WriteLine();
        if (string.IsNullOrEmpty(input)) return;

        // Check "ds" before "d" to avoid prefix collision
        if (TryParseSelection(input, "ds", status.Datasets.Count, out int idx))
        {
            Console.WriteLine("Downloading datasets");
            Console.WriteLine();
            await _downloader.DownloadDatasetAsync(status.Datasets[idx].Config);
        }
        else if (TryParseSelection(input, "s", status.Solvers.Count, out idx))
        {
            Console.WriteLine("Downloading solvers");
            Console.WriteLine();
            await _downloader.DownloadSolverAsync(status.Solvers[idx].Config);
        }
        else if (TryParseSelection(input, "d", status.Databases.Count, out idx))
        {
            Console.WriteLine("Downloading databases");
            Console.WriteLine();
            await _downloader.DownloadDatabaseAsync(status.Databases[idx].Config);
        }
        else if (TryParseSelection(input, "p", status.Profiles.Count, out idx))
        {
            var profile = status.Profiles[idx];
            Console.WriteLine("Downloading solvers");
            Console.WriteLine();
            await _downloader.DownloadSolverAsync(profile.Solver.Config);
            Console.WriteLine();
            Console.WriteLine("Downloading databases");
            Console.WriteLine();
            await _downloader.DownloadDatabaseAsync(profile.Database.Config);
            Console.WriteLine();
            Console.WriteLine("Done!");
        }
        else if (TryParseSelection(input, "r", status.Runs.Count, out idx))
        {
            var run = status.Runs[idx];
            Console.WriteLine("Downloading solvers");
            Console.WriteLine();
            await _downloader.DownloadSolverAsync(run.Profile.Solver.Config);
            Console.WriteLine("Downloading databases");
            Console.WriteLine();
            await _downloader.DownloadDatabaseAsync(run.Profile.Database.Config);
            Console.WriteLine("Downloading datasets");
            Console.WriteLine();
            await _downloader.DownloadDatasetAsync(run.Dataset.Config);
            Console.WriteLine("Done!");
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }

    private void HandleRunBenchmark(BenchmarkEnvironmentStatus status)
    {
        var availableRuns = status.Runs.Where(r => r.IsAvailable).ToList();
        if (availableRuns.Count == 0)
        {
            Console.WriteLine("No runs are available. Please ensure all required solvers, databases, and datasets are installed.");
            return;
        }

        Console.Write("Select run (rN): ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        Console.WriteLine();
        if (string.IsNullOrEmpty(input)) return;

        if (TryParseSelection(input, "r", availableRuns.Count, out var idx) || 
            TryParseSelection(input, "", availableRuns.Count, out idx))
        {
            var selectedRun = availableRuns[idx];
            _runner.PrintPreRunSummary(selectedRun);
            
            Console.WriteLine();
            Console.Write("Run the benchmark? (y/n): ");

            input = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (input != "y")
                return;
            
            Console.WriteLine();
            _runner.RunBenchmarking(selectedRun);
        }
        else
        {
            Console.WriteLine("Invalid selection.");
        }
    }

    private static bool TryParseSelection(string input, string prefix, int count, out int zeroBasedIndex)
    {
        zeroBasedIndex = -1;
        if (!input.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var numStr = input[prefix.Length..];
        if (!int.TryParse(numStr, out int num)) return false;
        if (num < 1 || num > count) return false;
        zeroBasedIndex = num - 1;
        return true;
    }
}
