namespace ConsoleSolverBenchmarkTool.Config;

public class BenchmarkConfig
{
    public List<SolverConfig> Solvers { get; set; } = [];
    public List<DatabaseConfig> Databases { get; set; } = [];
    public List<ProfileConfig> Profiles { get; set; } = [];
    public List<DatasetConfig> Datasets { get; set; } = [];
    public List<RunConfig> Runs { get; set; } = [];
}

public class SolverConfig
{
    public string Name { get; set; } = "";
    public string Directory { get; set; } = "";
    public Dictionary<string, string>? Downloads { get; set; }

    public string? GetDownloadUrl(string arch) =>
        Downloads != null && Downloads.TryGetValue(arch, out var url) ? url : null;

    public bool HasAnyDownloadsForArch(string arch) => GetDownloadUrl(arch) != null;
}

public class DatabaseConfig
{
    public string Name { get; set; } = "";
    public string Directory { get; set; } = "";
    public List<string>? Downloads { get; set; }

    public bool HasAnyDownloads => Downloads is { Count: > 0 };
}

public class ProfileConfig
{
    public string Name { get; set; } = "";
    public string Solver { get; set; } = "";
    public string Database { get; set; } = "";
}

public class DatasetConfig
{
    public string Name { get; set; } = "";
    public string Directory { get; set; } = "";
    public string Pattern { get; set; } = "";
    public List<string>? Downloads { get; set; }

    public bool HasAnyDownloads => Downloads is { Count: > 0 };
}

public class RunConfig
{
    public string Name { get; set; } = "";
    public string Dataset { get; set; } = "";
    public string Profile { get; set; } = "";
    public string OutputDirectory { get; set; } = "";
    public int Iterations { get; set; } = 1;
    public int MaxStars { get; set; } = 0;
    public List<int> SamplingVariations { get; set; } = [];
    public List<List<double>> RadiusVariations { get; set; } = [];
    public List<List<int>> OffsetVariations { get; set; } = [];
}
