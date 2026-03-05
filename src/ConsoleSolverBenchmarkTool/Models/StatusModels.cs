namespace ConsoleSolverBenchmarkTool.Models;

using ConsoleSolverBenchmarkTool.Config;

public class SolverWithStatus
{
    public required SolverConfig Config { get; init; }
    public bool IsInstalled { get; init; }

    // True if a download link exists for the current architecture
    public bool IsDownloadable { get; init; }
}

public class DatabaseWithStatus
{
    public required DatabaseConfig Config { get; init; }
    public bool IsInstalled { get; init; }

    // True if any download links are listed
    public bool IsDownloadable { get; init; }
}

public class DatasetWithStatus
{
    public required DatasetConfig Config { get; init; }
    public bool IsInstalled { get; init; }

    // True if any download links are listed
    public bool IsDownloadable { get; init; }
}

public class ProfileWithStatus
{
    public required ProfileConfig Config { get; init; }
    public required SolverWithStatus Solver { get; init; }
    public required DatabaseWithStatus Database { get; init; }
}

public class BenchmarkRunWithStatus
{
    public required RunConfig Config { get; init; }
    public required ProfileWithStatus Profile { get; init; }
    public required DatasetWithStatus Dataset { get; init; }

    public bool IsAvailable => Profile.Solver.IsInstalled && Profile.Database.IsInstalled && Dataset.IsInstalled;
}

public class BenchmarkEnvironmentStatus
{
    public required List<SolverWithStatus> Solvers { get; init; }
    public required List<DatabaseWithStatus> Databases { get; init; }
    public required List<ProfileWithStatus> Profiles { get; init; }
    public required List<DatasetWithStatus> Datasets { get; init; }
    public required List<BenchmarkRunWithStatus> Runs { get; init; }
}
