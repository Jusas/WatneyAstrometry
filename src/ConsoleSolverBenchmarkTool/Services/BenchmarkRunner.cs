using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using ConsoleSolverBenchmarkTool.Config;
using ConsoleSolverBenchmarkTool.UI;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace ConsoleSolverBenchmarkTool.Services;

using ConsoleSolverBenchmarkTool.Models;

public class BenchmarkRunner
{
    private class Variation
    {
        public int Sampling { get; set; }
        public double MinRadius { get; set; }
        public double MaxRadius { get; set; }
        public int LowerDensityOffset { get; set; }
        public int HigherDensityOffset { get; set; }
    }
    
    
    public void RunBenchmarking(BenchmarkRunWithStatus run)
    {
        var variations = BuildSettingVariations(run.Config);
        var tempConfigFilePath = SolverConfigFileGenerator.GenerateTempConfigForRun(run);
        var solverDirectory = run.Profile.Solver.Config.Directory;
        
        var startTimeStamp = $"{DateTime.Now:yyyy-MM-ddTHH-mm-ss}";
        var outputDir = Path.Combine(run.Config.OutputDirectory, $"run_{startTimeStamp}");
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var files = ExpandDatasetFiles(run.Dataset.Config.Directory, run.Dataset.Config.Pattern);
        var totalSolvesPerIteration = files.Length * variations.Count;
        
        var swTotal = Stopwatch.StartNew();
        
        var solverConfig = run.Profile.Solver.Config;
        var solverPrefix = solverConfig.Name;
        var report = new SolverBenchmarkReport()
        {
            SolverName = solverConfig.Name,
            SolverPrefix = solverPrefix,
            TimeStamp = startTimeStamp,
            SamplingVariations = run.Config.SamplingVariations.ToArray(),
            OffsetVariations = run.Config.OffsetVariations.Select(x => x.ToArray()).ToArray(),
            Iterations = run.Config.Iterations,
            RadiusVariations = run.Config.RadiusVariations.Select(x => x.ToArray()).ToArray(),
            ErrorCount = 0,
            ImageFilenameList = files.Select(Path.GetFileName).ToList()!
        };
            
        var swSolver = Stopwatch.StartNew();
        Console.WriteLine($"Benchmarking solver: {solverConfig.Name}");
        Console.WriteLine(ConsolePrint.BigSeparator + Environment.NewLine);

        var allSolvesForThisSolver = new List<CsvEntry>(); 
        
        for (var iteration = 0; iteration < run.Config.Iterations; iteration++)
        {
            var currentSolveInIteration = 1;
            Console.WriteLine(ConsolePrint.SmallSeparator);
            Console.WriteLine($"Iteration {iteration + 1}/{run.Config.Iterations}");
            Console.WriteLine(ConsolePrint.SmallSeparator + Environment.NewLine);
            
            Console.WriteLine($"Images to solve: {files.Length}" + Environment.NewLine);
            for (var imageIndex = 0; imageIndex < files.Length; imageIndex++)
            {
                var imageFile = files[imageIndex];
                var imageIterationOutputCsvFile = Path.Combine(outputDir,
                    $"{report.SolverPrefix}_{startTimeStamp}_{Path.GetFileName(imageFile)}__iter-{iteration:00}.csv");
                var csvRows = new List<string>()
                {
                    CsvEntry.CsvHeader
                };

                for (var variationIndex = 0; variationIndex < variations.Count; variationIndex++)
                {
                    // [00:00:00] iter[1/5] tot[5/891] img[1/9] var[5/81]>  samp: 1  offs: [1, 1]  radi: [0.5, 8.0]  => 00:00:01.554
                    var variation = variations[variationIndex];

                    Console.Write("[{0}] iter[{1}/{2}] tot[{3}/{4}] img[{5}/{6}] var[{7}/{8}]>  ",
                        swTotal.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
                        iteration + 1, run.Config.Iterations,
                        currentSolveInIteration, totalSolvesPerIteration,
                        imageIndex + 1, files.Length,
                        variationIndex + 1, variations.Count);
                    
                    Console.Write($"samp: {variation.Sampling}  " +
                                  $"offs: [{variation.LowerDensityOffset}, {variation.HigherDensityOffset}]  " +
                                  $"radi: [{variation.MinRadius:F1}, {variation.MaxRadius:F1}]  => ");

                    currentSolveInIteration++;
                    var variationArgs = CreateVariationArguments(variation);
                    var configFile = tempConfigFilePath;
                    var solverArgs = new[]
                    {
                        "blind",
                        "--use-config", configFile,
                        "--max-stars", run.Config.MaxStars.ToString(CultureInfo.InvariantCulture),
                        "--image", imageFile,
                        "--extended",
                        "--benchmark",
                        "--out-format", "json"
                    }.Concat(variationArgs);

                    var exeName = OperatingSystem.IsWindows() ? "watney-solve.exe" : "watney-solve";
                    var exePath = Path.Combine(solverDirectory, exeName);
                    if (!File.Exists(exePath))
                        exePath = Path.Combine(solverDirectory, "watney-cli", exeName);

                    var processStartInfo = new ProcessStartInfo(exePath, solverArgs)
                    {
                        CreateNoWindow = true,
                        WorkingDirectory = solverDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var process = new Process()
                    {
                        StartInfo = processStartInfo,
                        EnableRaisingEvents = true,
                    };

                    var processOutput = new List<string?>();
                    var csvEntry = new CsvEntry()
                    {
                        ImageName = Path.GetFileName(imageFile),
                        SolverName = run.Profile.Solver.Config.Name,
                        ArgsSampling = variation.Sampling,
                        ArgsLowerDensityOffset = variation.LowerDensityOffset,
                        ArgsHigherDensityOffset = variation.HigherDensityOffset,
                        ArgsMaxRadius = variation.MaxRadius,
                        ArgsMinRadius = variation.MinRadius
                    };

                    try
                    {
                        process.Start();
                        process.BeginOutputReadLine();
                        process.OutputDataReceived += (_, args) => processOutput.Add(args.Data);
                        process.WaitForExit();
                        if (process.ExitCode == 0)
                        {
                            ParseResult(processOutput, csvEntry);
                            Console.Write(
                                csvEntry.SolutionTotalTimeSpent.ToString(@"hh\:mm\:ss\.fff",
                                    CultureInfo.InvariantCulture) + Environment.NewLine);
                        }
                        else
                        {
                            report.ErrorCount++;
                            csvEntry.Errors = process.StandardError.ReadToEnd();
                            Console.WriteLine("Solver Error");
                            ConsolePrint.PrintSmallSeparator();
                            processOutput.ForEach(Console.WriteLine);
                            if (csvEntry.Errors.Length == 0)
                                csvEntry.Errors = processOutput.FirstOrDefault() ?? string.Empty;
                            ConsolePrint.PrintSmallSeparator();
                        }
                    }
                    catch (Exception e)
                    {
                        report.ErrorCount++;
                        csvEntry.Errors = e.Message;
                        Console.WriteLine("Exception: ");
                    }

                    allSolvesForThisSolver.Add(csvEntry);
                    csvRows.Add(csvEntry.AsCsv());
                }

                File.WriteAllLines(imageIterationOutputCsvFile, csvRows);
            }
        }

        swSolver.Stop();
        
        File.Delete(tempConfigFilePath);

        var allAveragedRows = allSolvesForThisSolver.GroupBy(x => x.GetId())
            .Select(group => AverageCompletedResults(group.ToArray()))
            .ToArray();
        var averagedRowsPerImage = allAveragedRows.GroupBy(x => x.ImageName);
        
        foreach (var rowsForImage in averagedRowsPerImage)
        {
            var averagedOutputCsvFile = Path.Combine(outputDir,
                $"{solverPrefix}_{startTimeStamp}_{rowsForImage.First().ImageName}__averaged.csv");
            File.WriteAllLines(averagedOutputCsvFile, new [] 
                { CsvEntry.CsvHeader }.Concat(rowsForImage.Select(row => row.AsCsv())));    
        }
        
        var reportFile = Path.Combine(outputDir,
            $"{solverPrefix}_{startTimeStamp}__report.txt");
        report.Write(swSolver.Elapsed, reportFile);
        
        Console.WriteLine($"Total time spent: {swSolver.Elapsed}");
        Console.WriteLine("Wrote report to " + reportFile);
        
        Console.WriteLine(Environment.NewLine + ConsolePrint.SmallSeparator + Environment.NewLine);
        Console.WriteLine("Benchmark run complete");
        Console.WriteLine("Results saved to output directory: " + outputDir);
    
    }
    
    private static string[] ExpandDatasetFiles(string directory, string pattern)
    {
        Matcher matcher = new Matcher();
        matcher.AddInclude(pattern);
        
        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directory)));
        if(!result.HasMatches)
            throw new Exception("No matching input files");

        return result.Files
            .Select(f => new FileInfo(Path.Combine(directory, f.Path)).FullName)
            .ToArray();
    }
    
    private CsvEntry AverageCompletedResults(CsvEntry[] csvRows)
    {
        return new CsvEntry()
        {
            ImageName = csvRows.First().ImageName,
            SolverName = csvRows.First().SolverName,
            ArgsSampling = csvRows.First().ArgsSampling,
            ArgsLowerDensityOffset = csvRows.First().ArgsLowerDensityOffset,
            ArgsHigherDensityOffset = csvRows.First().ArgsHigherDensityOffset,
            ArgsMaxRadius = csvRows.First().ArgsMaxRadius,
            ArgsMinRadius = csvRows.First().ArgsMinRadius,
            DurationImageQuadFormation = csvRows.Average(x => x.DurationImageQuadFormation),
            DurationImageRead = csvRows.Average(x => x.DurationImageRead),
            DurationSolve = csvRows.Average(x => x.DurationSolve),
            DurationStarDetection = csvRows.Average(x => x.DurationStarDetection),
            SolutionDec = csvRows.First().SolutionDec,
            SolutionRa = csvRows.First().SolutionRa,
            Errors = string.Join("; ", csvRows.Select(x => x.Errors)),
            SolutionDiscoverySearchRunCenter = csvRows.First().SolutionDiscoverySearchRunCenter,
            SolutionDiscoverySearchRunRadius = csvRows.First().SolutionDiscoverySearchRunRadius,
            SolutionFieldRadius = csvRows.First().SolutionFieldRadius,
            SolutionQuadMatches = csvRows.First().SolutionQuadMatches,
            SolutionSearchIterations = (int)csvRows.Average(x => x.SolutionSearchIterations),
            SolutionStarsDetected = csvRows.First().SolutionStarsDetected,
            SolutionStarsUsed = csvRows.First().SolutionStarsUsed,
            SolutionTotalTimeSpent = TimeSpan.FromSeconds(csvRows.Average(x => x.SolutionTotalTimeSpent.TotalSeconds)),
            Solved = csvRows.Any(x => x.Solved),
            SourceImageHeight = csvRows.First().SourceImageHeight,
            SourceImageWidth = csvRows.First().SourceImageWidth
        };
    }
    
    private void ParseResult(List<string?> resultOutput, CsvEntry csvEntry)
    {
        
        for (var i = resultOutput.Count - 1; i >= 0; i--)
        {
            if (resultOutput[i] == null)
            {
                resultOutput.RemoveAt(i);
                continue;
            }
            
            if (resultOutput[i].Trim() == "}")
                break;
        
            var line = resultOutput[i].Trim();
            if (line.StartsWith("IMAGEREAD_DURATION"))
                csvEntry.DurationImageRead = double.Parse(line.Split(":").Last().Trim());
            if (line.StartsWith("STARDETECTION_DURATION"))
                csvEntry.DurationStarDetection = double.Parse(line.Split(":").Last().Trim());
            if (line.StartsWith("QUADFORMATION_DURATION"))
                csvEntry.DurationImageQuadFormation = double.Parse(line.Split(":").Last().Trim());
            if (line.StartsWith("SOLVE_DURATION"))
                csvEntry.DurationSolve = double.Parse(line.Split(":").Last().Trim());

            resultOutput.RemoveAt(i);
        }

        var remainingJsonResult = string.Join("\n", resultOutput);

        var resultProperties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(remainingJsonResult);
        foreach (var property in resultProperties)
        {
            switch (property.Key)
            {
                case "success":
                    csvEntry.Solved = property.Value.GetBoolean();
                    break;
                case "ra":
                    csvEntry.SolutionRa = property.Value.GetDouble();
                    break;
                case "dec":
                    csvEntry.SolutionDec = property.Value.GetDouble();
                    break;
                case "fieldRadius":
                    csvEntry.SolutionFieldRadius = property.Value.GetDouble();
                    break;
                case "starsDetected":
                    csvEntry.SolutionStarsDetected = property.Value.GetInt32();
                    break;
                case "starsUsed":
                    csvEntry.SolutionStarsUsed = property.Value.GetInt32();
                    break;
                case "searchIterations":
                    csvEntry.SolutionSearchIterations = property.Value.GetInt32();
                    break;
                case "quadMatches":
                    csvEntry.SolutionQuadMatches = property.Value.GetInt32();
                    break;
                case "searchRunCenter":
                    csvEntry.SolutionDiscoverySearchRunCenter = property.Value.GetString()!;
                    break;
                case "searchRunRadius":
                    csvEntry.SolutionDiscoverySearchRunRadius = property.Value.GetDouble();
                    break;
                case "timeSpent":
                    csvEntry.SolutionTotalTimeSpent = TimeSpan.Parse(property.Value.GetString(), CultureInfo.InvariantCulture);
                    break;
                case "imageWidth":
                    csvEntry.SourceImageWidth = property.Value.GetInt32();
                    break;
                case "imageHeight":
                    csvEntry.SourceImageHeight = property.Value.GetInt32();
                    break;
                default:
                    break;
            }
                
        }

    }

    private List<Variation> BuildSettingVariations(RunConfig runConfig)
    {
        var variations = new List<Variation>();
        foreach (var sampling in runConfig.SamplingVariations)
        {
            foreach (var radius in runConfig.RadiusVariations)
            {
                var minRadius = Math.Min(radius[0], radius[1]);
                var maxRadius = Math.Max(radius[0], radius[1]);

                foreach (var offsets in runConfig.OffsetVariations)
                {
                    var lowerDensityOffset = offsets[0];
                    var higherDensityOffset = offsets[1];

                    variations.Add(new Variation
                    {
                        Sampling = sampling,
                        MinRadius = minRadius,
                        MaxRadius = maxRadius,
                        LowerDensityOffset = lowerDensityOffset,
                        HigherDensityOffset = higherDensityOffset
                    });
                }
            }
        }
        
        return variations;
    }

    private List<string> CreateVariationArguments(Variation variation)
    {
        return new List<string>()
        {
            "--lower-density-offset", variation.LowerDensityOffset.ToString(),
            "--higher-density-offset", variation.HigherDensityOffset.ToString(),
            "--min-radius", variation.MinRadius.ToString("F1", CultureInfo.InvariantCulture),
            "--max-radius", variation.MaxRadius.ToString("F1", CultureInfo.InvariantCulture),
            "--sampling", variation.Sampling.ToString(),
        };
    }

    public void PrintPreRunSummary(BenchmarkRunWithStatus run)
    {
        var files = ExpandDatasetFiles(run.Dataset.Config.Directory, run.Dataset.Config.Pattern);
        
        Console.WriteLine($"About to run benchmark '{run.Config.Name}");
        Console.WriteLine($"- Uses dataset: '{run.Config.Dataset}'");
        Console.WriteLine($"- Uses solver: '{run.Profile.Solver.Config.Name}'");
        Console.WriteLine($"- Uses database: '{run.Profile.Database.Config.Name}'");

        Console.WriteLine(Environment.NewLine + "Images to benchmark:");
        foreach (var imageFile in files)
            Console.WriteLine($"- {imageFile}");

        var totalVariations = run.Config.SamplingVariations.Count *
            run.Config.OffsetVariations.Count * run.Config.RadiusVariations.Count;
        
        Console.WriteLine(Environment.NewLine + "Setting variations:");
        Console.WriteLine($"- Sampling variations: {run.Config.SamplingVariations.Count}");
        Console.WriteLine($"- Radius variations: {run.Config.RadiusVariations.Count}");
        Console.WriteLine($"- Offset variations: {run.Config.OffsetVariations.Count}");
        Console.WriteLine($"= Total variations per image: {totalVariations}");
        
        Console.WriteLine(Environment.NewLine + 
            $"Total solver runs: {totalVariations * files.Length}");
        Console.WriteLine(Environment.NewLine + 
            $"Results will be saved to a new directory in: {run.Config.OutputDirectory}");

    }
    
}
