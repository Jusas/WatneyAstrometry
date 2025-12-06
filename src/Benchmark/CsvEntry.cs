// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;

namespace Benchmark;

public class CsvEntry
{
    public required string ImageName { get; set; }
    public required string SolverName { get; set; } 
    public int ArgsSampling { get; set; }
    public double ArgsMinRadius { get; set; }
    public double ArgsMaxRadius { get; set; }
    public int ArgsLowerDensityOffset { get; set; }
    public int ArgsHigherDensityOffset { get; set; }
    public int SourceImageWidth { get; set; }
    public int SourceImageHeight { get; set; }
    public bool Solved { get; set; }
    public double DurationImageRead { get; set; }
    public double DurationStarDetection { get; set; }
    public double DurationImageQuadFormation { get; set; }
    public double DurationSolve { get; set; }
    public string SolutionDiscoverySearchRunCenter { get; set; } = string.Empty;
    public double SolutionDiscoverySearchRunRadius { get; set; }
    public int SolutionQuadMatches { get; set; }
    public double SolutionRa { get; set; }
    public double SolutionDec { get; set; }
    public double SolutionFieldRadius { get; set; }
    public int SolutionStarsDetected { get; set; }
    public int SolutionStarsUsed { get; set; }
    public TimeSpan SolutionTotalTimeSpent { get; set; }
    public int SolutionSearchIterations { get; set; }
    public string Errors { get; set; } = string.Empty;

    private static string Separator = "|";
    public static string CsvHeader =>
        string.Join(Separator,
            nameof(ImageName),
            nameof(SolverName),
            nameof(ArgsSampling),
            nameof(ArgsMinRadius),
            nameof(ArgsMaxRadius),
            nameof(ArgsLowerDensityOffset),
            nameof(ArgsHigherDensityOffset),
            nameof(SourceImageWidth),
            nameof(SourceImageHeight),
            nameof(Solved),
            nameof(DurationImageRead),
            nameof(DurationStarDetection),
            nameof(DurationImageQuadFormation),
            nameof(DurationSolve),
            nameof(SolutionDiscoverySearchRunCenter),
            nameof(SolutionDiscoverySearchRunRadius),
            nameof(SolutionQuadMatches),
            nameof(SolutionRa),
            nameof(SolutionDec),
            nameof(SolutionFieldRadius),
            nameof(SolutionStarsDetected),
            nameof(SolutionStarsUsed),
            nameof(SolutionTotalTimeSpent),
            nameof(SolutionSearchIterations),
            nameof(Errors));
    
    public string AsCsv()
    {
        var culture = CultureInfo.InvariantCulture;
        return string.Join(Separator,
            ImageName,
            SolverName,
            ArgsSampling,
            ArgsMinRadius.ToString(culture),
            ArgsMaxRadius.ToString(culture),
            ArgsLowerDensityOffset,
            ArgsHigherDensityOffset,
            SourceImageWidth,
            SourceImageHeight,
            Solved,
            DurationImageRead.ToString(culture),
            DurationStarDetection.ToString(culture),
            DurationImageQuadFormation.ToString(culture),
            DurationSolve.ToString(culture),
            SolutionDiscoverySearchRunCenter,
            SolutionDiscoverySearchRunRadius.ToString(culture),
            SolutionQuadMatches,
            SolutionRa.ToString(culture),
            SolutionDec.ToString(culture),
            SolutionFieldRadius.ToString(culture),
            SolutionStarsDetected,
            SolutionStarsUsed,
            SolutionTotalTimeSpent.ToString(@"hh\:mm\:ss\.fff", culture),
            SolutionSearchIterations,
            Errors);
    }
}