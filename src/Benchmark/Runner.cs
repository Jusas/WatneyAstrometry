using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Benchmark;

public class Runner
{
    private readonly BenchmarkConfig _benchmarkConfig;

    private class Variation
    {
        public int Sampling { get; set; }
        public double MinRadius { get; set; }
        public double MaxRadius { get; set; }
        public int LowerDensityOffset { get; set; }
        public int HigherDensityOffset { get; set; }
    }
    
    public Runner(BenchmarkConfig benchmarkConfig)
    {
        _benchmarkConfig = benchmarkConfig;
    }

    private string BigSeparator => string.Join("", Enumerable.Range(0, 80).Select(_ => "="));
    private string SmallSeparator => string.Join("", Enumerable.Range(0, 80).Select(_ => "-"));

    public void RunBlindBenchmarking()
    {
        var blindConfig = _benchmarkConfig.Blind;
        var solvers = _benchmarkConfig.Blind.WatneySolvers;
        var files = blindConfig.Files;
        var variations = BuildSettingVariations();

        if (!Directory.Exists(blindConfig.OutputDir))
            Directory.CreateDirectory(blindConfig.OutputDir);

        var startTimeStamp = $"{DateTime.Now:yyyy-MM-ddTHH-mm-ss}";
        
        var totalSolvesPerIteration = files.Length * variations.Count;
        
        var swTotal = Stopwatch.StartNew();
        
        for (var solverIndex = 0; solverIndex < solvers.Length; solverIndex++)
        {
            var solver = solvers[solverIndex];
            var report = new SolverBenchmarkReport()
            {
                SolverName = solver.Name,
                SolverPrefix = solver.Prefix,
                TimeStamp = startTimeStamp,
                SamplingVariations = _benchmarkConfig.Blind.SamplingVariations,
                OffsetVariations = _benchmarkConfig.Blind.OffsetVariations,
                Iterations = solver.Iterations,
                RadiusVariations = _benchmarkConfig.Blind.RadiusVariations,
                ErrorCount = 0,
                ImageFilenameList = files.Select(Path.GetFileName).ToList()!
            };
                
            var swSolver = Stopwatch.StartNew();
            Console.WriteLine($"Benchmarking solver: {solver.Name} ({solverIndex+1}/{solvers.Length})");
            Console.WriteLine(BigSeparator + Environment.NewLine);

            var allSolvesForThisSolver = new List<CsvEntry>(); 
            
            for (var iteration = 0; iteration < solver.Iterations; iteration++)
            {
                var currentSolveInIteration = 1;
                Console.WriteLine(SmallSeparator);
                Console.WriteLine($"Iteration {iteration + 1}/{solver.Iterations}");
                Console.WriteLine(SmallSeparator + Environment.NewLine);
                
                Console.WriteLine($"Images to solve: {files.Length}" + Environment.NewLine);
                for (var imageIndex = 0; imageIndex < files.Length; imageIndex++)
                {
                    var imageFile = files[imageIndex];
                    var imageIterationOutputCsvFile = Path.Combine(blindConfig.OutputDir,
                        $"{solver.Prefix}_{startTimeStamp}_{Path.GetFileName(imageFile)}__iter-{iteration:00}.csv");
                    var csvRows = new List<string>()
                    {
                        CsvEntry.CsvHeader
                    };

                    for (var variationIndex = 0; variationIndex < variations.Count; variationIndex++)
                    {
                        // [00:00:00] sol[1/1] itr[1/5] tot[5/891] img[1/9] var[5/81]>  samp: 1  offs: [1, 1]  radi: [0.5, 8.0]  => 00:00:01.554
                        var variation = variations[variationIndex];

                        Console.Write("[{0}] sol[{1}/{2}] itr[{3}/{4}] tot[{5}/{6}] img[{7}/{8}] var[{9}/{10}]]>  ",
                            swTotal.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
                            solverIndex + 1, solvers.Length,
                            iteration + 1, solver.Iterations,
                            currentSolveInIteration, totalSolvesPerIteration,
                            imageIndex + 1, files.Length,
                            variationIndex + 1, variations.Count);
                        
                        Console.Write($"samp: {variation.Sampling}  " +
                                      $"offs: [{variation.LowerDensityOffset}, {variation.HigherDensityOffset}]  " +
                                      $"radi: [{variation.MinRadius:F1}, {variation.MaxRadius:F1}]  => ");

                        currentSolveInIteration++;
                        var variationArgs = CreateVariationArguments(variation);
                        var configFile =
                            new FileInfo(Path.Combine(BenchmarkConfig.DataRootDirectory, solver.ConfigFile)).FullName;
                        var solverArgs = new[]
                        {
                            "blind",
                            "--use-config", configFile,
                            "--image", imageFile,
                            "--extended",
                            "--benchmark",
                            "--out-format", "json"
                        }.Concat(variationArgs);

                        var exeName = OperatingSystem.IsWindows() ? "watney-solve.exe" : "watney-solve";
                        exeName = Path.Combine(BenchmarkConfig.DataRootDirectory, solver.Dir, exeName);

                        var processStartInfo = new ProcessStartInfo(exeName, solverArgs)
                        {
                            CreateNoWindow = true,
                            WorkingDirectory = BenchmarkConfig.DataRootDirectory,
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
                            SolverName = solver.Name,
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
                                Console.Write("Solver Error" + Environment.NewLine);
                                Console.WriteLine(SmallSeparator);
                                processOutput.ForEach(line => Console.WriteLine(line));
                                if (csvEntry.Errors.Length == 0)
                                    csvEntry.Errors = processOutput.FirstOrDefault() ?? string.Empty;
                                Console.WriteLine(SmallSeparator);

                            }
                        }
                        catch (Exception e)
                        {
                            report.ErrorCount++;
                            csvEntry.Errors = e.Message;
                            Console.Write("Exception" + Environment.NewLine);
                        }

                        allSolvesForThisSolver.Add(csvEntry);
                        csvRows.Add(csvEntry.AsCsv());
                    }

                    File.WriteAllLines(imageIterationOutputCsvFile, csvRows);
                }
            }

            swSolver.Stop();

            var allAveragedRows = allSolvesForThisSolver.GroupBy(x => x.GetId())
                .Select(group => AverageCompletedResults(group.ToArray()))
                .ToArray();
            var averagedRowsPerImage = allAveragedRows.GroupBy(x => x.ImageName);
            
            foreach (var rowsForImage in averagedRowsPerImage)
            {
                var averagedOutputCsvFile = Path.Combine(blindConfig.OutputDir,
                    $"{solver.Prefix}_{startTimeStamp}_{rowsForImage.First().ImageName}__averaged.csv");
                File.WriteAllLines(averagedOutputCsvFile, new [] 
                    { CsvEntry.CsvHeader }.Concat(rowsForImage.Select(row => row.AsCsv())));    
            }
            
            var reportFile = Path.Combine(blindConfig.OutputDir,
                $"{solver.Prefix}_{startTimeStamp}__report.txt");
            report.Write(swSolver.Elapsed, reportFile);
            
            Console.WriteLine($"Total time spent: {swSolver.Elapsed}");
            Console.WriteLine("Wrote report to " + reportFile);
            
            Console.WriteLine(Environment.NewLine + SmallSeparator + Environment.NewLine);
            Console.WriteLine("Benchmark run complete");
            Console.WriteLine("Results saved to output directory: " + blindConfig.OutputDir);
        }
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

    private List<Variation> BuildSettingVariations()
    {
        var variations = new List<Variation>();
        foreach (var sampling in _benchmarkConfig.Blind.SamplingVariations)
        {
            foreach (var radius in _benchmarkConfig.Blind.RadiusVariations)
            {
                var minRadius = Math.Min(radius[0], radius[1]);
                var maxRadius = Math.Max(radius[0], radius[1]);

                foreach (var offsets in _benchmarkConfig.Blind.OffsetVariations)
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

    public void PrintSummary()
    {
        var blindConfig = _benchmarkConfig.Blind;
        var files = blindConfig.Files;
        
        Console.WriteLine("About to run benchmark for solvers: ");
        foreach (var solver in blindConfig.WatneySolvers)
            Console.WriteLine($"- {solver.Name}");            

        Console.WriteLine(Environment.NewLine + "Images to benchmark:");
        foreach (var imageFile in files)
            Console.WriteLine($"- {imageFile}");

        var totalVariations = blindConfig.SamplingVariations.Length *
            blindConfig.OffsetVariations.Length *
            blindConfig.OffsetVariations.Length;
        
        Console.WriteLine(Environment.NewLine + "Setting variations:");
        Console.WriteLine($"- Sampling variations: {blindConfig.SamplingVariations.Length}");
        Console.WriteLine($"- Radius variations: {blindConfig.RadiusVariations.Length}");
        Console.WriteLine($"- Offset variations: {blindConfig.OffsetVariations.Length}");
        Console.WriteLine($"= Total variations per image: {totalVariations}");
        
        Console.WriteLine(Environment.NewLine + 
            $"Total solver runs: {blindConfig.WatneySolvers.Length * totalVariations * files.Length}");
        Console.WriteLine(Environment.NewLine + 
            $"Results will be saved to: {blindConfig.OutputDir}");

    }
    
    
    
    
}