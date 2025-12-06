// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Benchmark;


public class WatneySolverEntry
{
    public required string Prefix { get; set; }
    public required string Name { get; set; }
    public required string Dir { get; set; }
    public required string ConfigFile { get; set; }
}

public class Blind
{
    public required string[] Files { get; set; }
    public required int[] SamplingVariations { get; set; }
    public required double[][] RadiusVariations { get; set; }
    public required int[][] OffsetVariations { get; set; }
    public required WatneySolverEntry[] WatneySolvers { get; set; }
    public required string OutputDir { get; set; }
}


public class BenchmarkConfig
{
    public required Blind Blind { get; set; }
    
    public static string DataRootDirectory { get; set; }

    public static BenchmarkConfig FromJson(Stream stream)
    {
        var jsonNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = jsonNamingPolicy,
            AllowTrailingCommas = true
        };
        var config = JsonSerializer.Deserialize<BenchmarkConfig>(stream, jsonSerializerOptions)!;
        Validate(config);
        
        return config;
    }

    public static BenchmarkConfig Validate(BenchmarkConfig benchmarkConfig)
    {
        if (benchmarkConfig.Blind.Files.Length == 0)
            throw new Exception("No input files");
        
        ExpandFilesAndPaths(benchmarkConfig);

        return benchmarkConfig;
    }

    public static void ExpandFilesAndPaths(BenchmarkConfig benchmarkConfig)
    {
        Matcher matcher = new Matcher();
        foreach (var imageFile in benchmarkConfig.Blind.Files)
            matcher.AddInclude(Path.Combine(DataRootDirectory, imageFile));

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(".")));
        if(!result.HasMatches)
            throw new Exception("No matching input files");

        benchmarkConfig.Blind.Files = result.Files
            .Select(f => new FileInfo(f.Path).FullName)
            .ToArray();
        
        var outputDirectoryInfo = new DirectoryInfo(Path.Combine(DataRootDirectory, benchmarkConfig.Blind.OutputDir));
        benchmarkConfig.Blind.OutputDir = outputDirectoryInfo.FullName;

    }
}
