namespace ConsoleSolverBenchmarkTool.Services;

using System.Runtime.InteropServices;
using ConsoleSolverBenchmarkTool.Config;
using ConsoleSolverBenchmarkTool.Models;

public class EnvironmentChecker
{
    private static readonly string[] ImageExtensions = [".fits", ".fit", ".png", ".jpg"];

    public string CurrentArchitecture { get; } = GetArchitectureString();

    public BenchmarkEnvironmentStatus Check(BenchmarkConfig config)
    {
        try
        {
            var solvers = config.Solvers.Select(CheckSolver).ToList();
            var databases = config.Databases.Select(CheckDatabase).ToList();
            var datasets = config.Datasets.Select(CheckDataset).ToList();

            var solverMap = solvers.ToDictionary(s => s.Config.Name);
            var databaseMap = databases.ToDictionary(d => d.Config.Name);
            var datasetMap = datasets.ToDictionary(ds => ds.Config.Name);

            var profiles = config.Profiles.Select(p =>
            {
                if (!solverMap.TryGetValue(p.Solver, out var solver))
                    throw new Exception($"Solver '{p.Solver}' referenced by profile '{p.Name}' not found in config");
                if (!databaseMap.TryGetValue(p.Database, out var database))
                    throw new Exception($"Database '{p.Database}' referenced by profile '{p.Name}' not found in config");
                return new ProfileWithStatus { Config = p, Solver = solver, Database = database };
            }).ToList();

            var profileMap = profiles.ToDictionary(p => p.Config.Name);

            var runs = config.Runs.Select(r =>
            {
                if (!profileMap.TryGetValue(r.Profile, out var profile))
                    throw new Exception($"Profile '{r.Profile}' referenced by run '{r.Name}' not found in config");
                if (!datasetMap.TryGetValue(r.Dataset, out var dataset))
                    throw new Exception($"Dataset '{r.Dataset}' referenced by run '{r.Name}' not found in config");
                return new BenchmarkRunWithStatus { Config = r, Profile = profile, Dataset = dataset };
            }).ToList();

            return new BenchmarkEnvironmentStatus
            {
                Solvers = solvers,
                Databases = databases,
                Profiles = profiles,
                Datasets = datasets,
                Runs = runs,
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error checking environment: {ex.Message}");
            throw new Exception("Failed to determine environment status", ex);
        }
    }

    private SolverWithStatus CheckSolver(SolverConfig config)
    {
        try
        {
            var dir = config.Directory;
            var isInstalled = Directory.Exists(dir) &&
                (File.Exists(Path.Combine(dir, "watney-solve")) ||
                 File.Exists(Path.Combine(dir, "watney-cli", "watney-solve")) ||
                 File.Exists(Path.Combine(dir, "watney-solve.exe")) ||
                 File.Exists(Path.Combine(dir, "watney-cli", "watney-solve.exe")));

            var isDownloadable = config.HasAnyDownloadsForArch(CurrentArchitecture);

            return new SolverWithStatus
            {
                Config = config,
                IsInstalled = isInstalled,
                IsDownloadable = isDownloadable,
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error checking solver '{config.Name}': {ex.Message}");
            throw new Exception($"Failed to check solver '{config.Name}'", ex);
        }
    }

    private static DatabaseWithStatus CheckDatabase(DatabaseConfig config)
    {
        try
        {
            bool isInstalled = false;
            if (Directory.Exists(config.Directory))
            {
                var files = Directory.GetFiles(config.Directory);
                isInstalled = files.Any(f => f.EndsWith(".qdb", StringComparison.OrdinalIgnoreCase)) &&
                              files.Any(f => f.EndsWith(".qdbindex", StringComparison.OrdinalIgnoreCase));
            }

            return new DatabaseWithStatus
            {
                Config = config,
                IsInstalled = isInstalled,
                IsDownloadable = config.HasAnyDownloads,
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error checking database '{config.Name}': {ex.Message}");
            throw new Exception($"Failed to check database '{config.Name}'", ex);
        }
    }

    private static DatasetWithStatus CheckDataset(DatasetConfig config)
    {
        try
        {
            bool isInstalled = false;
            if (Directory.Exists(config.Directory))
            {
                isInstalled = Directory.GetFiles(config.Directory)
                    .Any(f => ImageExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            }

            return new DatasetWithStatus
            {
                Config = config,
                IsInstalled = isInstalled,
                IsDownloadable = config.HasAnyDownloads,
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error checking dataset '{config.Name}': {ex.Message}");
            throw new Exception($"Failed to check dataset '{config.Name}'", ex);
        }
    }

    private static string GetArchitectureString()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win_arm64" : "win_x64";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx_arm64" : "osx_x64";

        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "linux_arm64",
            Architecture.Arm => "linux_arm",
            _ => "linux_x64",
        };
    }
}
