namespace Benchmark;

public class SolverBenchmarkReport
{

    public required string SolverName { get; set; }
    public required string SolverPrefix { get; set; }
    public required string TimeStamp { get; set; }
    public required List<string> ImageFilenameList { get; set; }
    public required int[] SamplingVariations { get; set; }
    public required double[][] RadiusVariations { get; set; }
    public required int[][] OffsetVariations { get; set; }
    public int Iterations { get; set; }
    public int ErrorCount { get; set; }
    
    
    public void Write(TimeSpan duration, string filename)
    {
        var procArch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
        var osArch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
        var osDesc = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var osVer = Environment.OSVersion;
        var machineName = Environment.MachineName;
        var cpuCount = Environment.ProcessorCount;
        
        var osName = OperatingSystem.IsLinux() 
            ? "Linux" : OperatingSystem.IsMacOS() 
                ? "Mac" : OperatingSystem.IsWindows() 
                    ? "Windows" : "Unknown";
        
        using var stream = File.OpenWrite(filename);
        using var writer = new StreamWriter(stream);
        writer.WriteLine("[Watney Benchmark Report]");
        writer.WriteLine($"SolverName={SolverName}");
        writer.WriteLine($"SolverPrefix={SolverPrefix}");
        writer.WriteLine($"TimeStamp={TimeStamp}");
        writer.WriteLine();
        writer.WriteLine("[SystemInfo]");
        writer.WriteLine($"MachineName={machineName}");
        writer.WriteLine($"OSIdentifiedName={osName}");
        writer.WriteLine($"OSArchitecture={osArch}");
        writer.WriteLine($"OSDescription={osDesc}");
        writer.WriteLine($"ProcessArchitecture={procArch}");
        writer.WriteLine($"Platform={osVer.VersionString}");
        writer.WriteLine($"ProcessorCount={cpuCount}");
        writer.WriteLine();
        writer.WriteLine("[BenchmarkInfo]");
        writer.WriteLine($"Duration={duration:c}");
        writer.WriteLine($"Iterations={Iterations}");
        writer.WriteLine($"ErrorCount={ErrorCount}");
        writer.WriteLine($"SamplingVariations={string.Join(", ", SamplingVariations)}");
        writer.WriteLine($"OffsetVariations={string.Join(", ", OffsetVariations.Select(o => $"[{o[0]},{o[1]}]"))}");
        writer.WriteLine($"RadiusVariations={string.Join(", ", RadiusVariations.Select(o => $"[{o[0]},{o[1]}]"))}");
        writer.WriteLine();
        writer.WriteLine("[Images]");
        ImageFilenameList.ForEach(f => writer.WriteLine($"Filename={f}"));
    }
    
}