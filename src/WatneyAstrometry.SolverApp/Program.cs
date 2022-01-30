// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using WatneyAstrometry.Core;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.QuadDb;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.ImageReaders;

namespace WatneyAstrometry.SolverApp
{
    class Program
    {

        public const string ApplicationName = "Watney Astrometric Solver";

        private static ParserResult<object> _parserResult;
        private static Configuration _configuration;
        private static IVerboseLogger _verboseLogger;
        private static bool _benchmarkMode = false;

        private static Stopwatch _starDetectionStopwatch = new();
        private static Stopwatch _solveProcessStopwatch = new();
        private static Stopwatch _imageReadStopwatch = new();
        private static Stopwatch _quadFormationStopwatch = new();

        public static void Main(string[] args)
        {
            // Enforce this, otherwise help texts will vary depending on culture and we don't want that.
            // Is this a bug in the command line parser 2.8?
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var parser = new CommandLine.Parser(config =>
            {
                config.HelpWriter = null;
                config.ParsingCulture = CultureInfo.InvariantCulture;
            });
            _parserResult = parser.ParseArguments<BlindOptions, NearbyOptions>(args);

            _parserResult
                .WithParsed<BlindOptions>(RunBlindSolve)
                .WithParsed<NearbyOptions>(RunNearbySolve)
                .WithNotParsed(errors => ErrorAction(_parserResult, errors, null));

            stopwatch.Stop();
            if (_benchmarkMode)
            {
                Console.WriteLine($"IMAGEREAD_DURATION: {_imageReadStopwatch.Elapsed.TotalSeconds}");
                Console.WriteLine($"STARDETECTION_DURATION: {_starDetectionStopwatch.Elapsed.TotalSeconds}");
                Console.WriteLine($"QUADFORMATION_DURATION: {_quadFormationStopwatch.Elapsed.TotalSeconds}");
                Console.WriteLine($"SOLVE_DURATION: {_solveProcessStopwatch.Elapsed.TotalSeconds}");
                Console.WriteLine($"FULL_DURATION: {stopwatch.Elapsed.TotalSeconds}");
            }
            
            Environment.Exit(0);
        }


        private static void LoadConfiguration(GenericOptions options)
        {
            var executableDir =
                Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

            var configFile = !string.IsNullOrEmpty(options.ConfigPath)
                ? options.ConfigPath
                : Path.Combine(executableDir, "watney-solve-config.yml");
            _configuration = Configuration.Load(configFile);
        }

        private static IVerboseLogger GetLogger(GenericOptions options)
        {
            if (options.LogToStdout || !string.IsNullOrEmpty(options.LogToFile))
            {
                _verboseLogger = new DefaultVerboseLogger(new DefaultVerboseLogger.Options
                {
                    Enabled = true,
                    WriteToFile = !string.IsNullOrEmpty(options.LogToFile),
                    LogFile = options.LogToFile,
                    WriteToStdout = options.LogToStdout
                });
            }

            return _verboseLogger;
        }

        private static SolverOptions ParseSolverOptions(GenericOptions options)
        {
            var solverOpts = new SolverOptions();
            if (options.MaxStars > 0)
                solverOpts.UseMaxStars = options.MaxStars;

            if (options.Sampling >= 1)
                solverOpts.UseSampling = options.Sampling;

            return solverOpts;
        }

        private static void RunBlindSolve(BlindOptions options)
        {
            Validate(options);
            LoadConfiguration(options);

            var strategy = ParseStrategy(options);
            var quadDatabase = new CompactQuadDatabase();
            
            var solver = new Solver(GetLogger(options))
                .UseImageReader<CommonFormatsImageReader>(() => new CommonFormatsImageReader(), CommonFormatsImageReader.SupportedImageExtensions)
                .UseQuadDatabase(() => quadDatabase.UseDataSource(_configuration.QuadDatabasePath));
            solver.OnSolveProgress += BenchmarkProgressHandler;

            var solverOptions = ParseSolverOptions(options);
            var solveTask = Task.Run(async () => await solver.SolveFieldAsync(options.ImageFilename, strategy, solverOptions, CancellationToken.None));
            solveTask.Wait();
            
            quadDatabase.Dispose();

            var result = solveTask.Result;
            SaveOutput(result, options, options.ExtendedOutput);
            
        }

        private static void RunNearbySolve(NearbyOptions options)
        {
            Validate(options);
            LoadConfiguration(options);

            var strategy = ParseStrategy(options);

            var quadDatabase = new CompactQuadDatabase();
            var solver = new Solver(GetLogger(options))
                .UseImageReader<CommonFormatsImageReader>(() => new CommonFormatsImageReader(), CommonFormatsImageReader.SupportedImageExtensions)
                .UseQuadDatabase(() => quadDatabase.UseDataSource(_configuration.QuadDatabasePath));
            solver.OnSolveProgress += BenchmarkProgressHandler;

            var solverOptions = ParseSolverOptions(options);
            var solveTask = Task.Run(async () => await solver.SolveFieldAsync(options.ImageFilename, strategy, solverOptions, CancellationToken.None));
            solveTask.Wait();

            quadDatabase.Dispose();

            var result = solveTask.Result;
            SaveOutput(result, options, options.ExtendedOutput);
            
        }

        private static void BenchmarkProgressHandler(SolverStep step)
        {
            if(step == SolverStep.SolveProcessStarted)
                _solveProcessStopwatch.Start();
            else if(step == SolverStep.SolveProcessFinished)
                _solveProcessStopwatch.Stop();
            else if(step == SolverStep.StarDetectionStarted)
                _starDetectionStopwatch.Start();
            else if(step == SolverStep.StarDetectionFinished)
                _starDetectionStopwatch.Stop();
            else if(step == SolverStep.ImageReadStarted)
                _imageReadStopwatch.Start();
            else if(step == SolverStep.ImageReadFinished)
                _imageReadStopwatch.Stop();
            else if(step == SolverStep.QuadFormationStarted)
                _quadFormationStopwatch.Start();
            else if(step == SolverStep.QuadFormationFinished)
                _quadFormationStopwatch.Stop();

        }

        private static void SaveOutput(SolveResult result, GenericOptions options, bool extended)
        {
            var outputData = new Dictionary<string, object>
            {
                ["success"] = result.Success
            };

            if (result.Success)
            {
                outputData.Add("ra", result.Solution.PlateCenter.Ra);
                outputData.Add("dec", result.Solution.PlateCenter.Dec);
                outputData.Add("fieldRadius", result.Solution.Radius);
                outputData.Add("orientation", result.Solution.Orientation);
                outputData.Add("pixScale", result.Solution.PixelScale);
                outputData.Add("parity", result.Solution.Parity.ToString().ToLowerInvariant());
            }

            if (extended)
            {
                outputData.Add("starsDetected", result.StarsDetected);
                outputData.Add("starsUsed", result.StarsUsedInSolve);
                outputData.Add("timeSpent", result.TimeSpent.ToString());
                outputData.Add("searchIterations", result.AreasSearched);
                if (result.Success)
                {
                    outputData.Add("imageWidth", result.Solution.ImageWidth);
                    outputData.Add("imageHeight", result.Solution.ImageHeight);
                    outputData.Add("searchRunCenter", result.SearchRun.Center.ToString());
                    outputData.Add("searchRunRadius", result.SearchRun.RadiusDegrees);
                    outputData.Add("quadMatches", result.MatchedQuads);
                    outputData.Add("fieldWidth", result.Solution.FieldWidth);
                    outputData.Add("fieldHeight", result.Solution.FieldHeight);
                    outputData.Add("fits_cd1_1", result.Solution.FitsHeaders.CD1_1);
                    outputData.Add("fits_cd1_2", result.Solution.FitsHeaders.CD1_2);
                    outputData.Add("fits_cd2_1", result.Solution.FitsHeaders.CD2_1);
                    outputData.Add("fits_cd2_2", result.Solution.FitsHeaders.CD2_2);
                    outputData.Add("fits_cdelt1", result.Solution.FitsHeaders.CDELT1);
                    outputData.Add("fits_cdelt2", result.Solution.FitsHeaders.CDELT2);
                    outputData.Add("fits_crota1", result.Solution.FitsHeaders.CROTA1);
                    outputData.Add("fits_crota2", result.Solution.FitsHeaders.CROTA2);
                    outputData.Add("fits_crpix1", result.Solution.FitsHeaders.CRPIX1);
                    outputData.Add("fits_crpix2", result.Solution.FitsHeaders.CRPIX2);
                    outputData.Add("fits_crval1", result.Solution.FitsHeaders.CRVAL1);
                    outputData.Add("fits_crval2", result.Solution.FitsHeaders.CRVAL2);
                }
            }

            string outputText = "";
            if (options.OutFormat == "json")
            {
                outputText = JsonConvert.SerializeObject(outputData, Formatting.Indented);
            }
            else if (options.OutFormat == "tsv")
            {
                outputText = string.Join("\n", 
                    outputData.Select(item => $"{item.Key}\t{TsvStringFormatter(item.Value)}"));
            }

            if(string.IsNullOrEmpty(options.OutFile))
                Console.WriteLine(outputText);
            else
            {
                var directory = Path.GetDirectoryName(options.OutFile);
                if(!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(Path.GetDirectoryName(options.OutFile));
                File.WriteAllText(options.OutFile, outputText, Encoding.ASCII);
            }

            if (result.Success && !string.IsNullOrEmpty(options.WcsFile))
            {
                var directory = Path.GetDirectoryName(options.WcsFile);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(Path.GetDirectoryName(options.WcsFile));
                using (var wcsStream = new FileStream(options.WcsFile, FileMode.Create))
                {
                    var wcsWriter = new WcsFitsWriter(wcsStream);
                    wcsWriter.WriteWcsFile(result.Solution.FitsHeaders, result.Solution.ImageWidth, result.Solution.ImageHeight);
                }
                
            }
        }

        // To ensure local cultures don't interfere. Json is culturally invariant, but this custom bare-bones TSV need to take it into account.
        private static string TsvStringFormatter(object obj)
        {
            var c = CultureInfo.InvariantCulture;
            if (obj is bool b)
                return b.ToString().ToLowerInvariant();
            if (obj is double d)
                return d.ToString(c);
            if (obj is float f)
                return f.ToString(c);
            return obj.ToString();
            
        }

        private static BlindSearchStrategy ParseStrategy(BlindOptions options)
        {
            var strategyOptions = new BlindSearchStrategyOptions()
            {
                MaxNegativeDensityOffset = options.LowerDensityOffset,
                MaxPositiveDensityOffset = options.HigherDensityOffset,
                UseParallelism = options.UseParallelism,
                MinRadiusDegrees = options.MinRadius,
                StartRadiusDegrees = options.MaxRadius,
                SearchOrderRa = options.WestFirst
                    ? BlindSearchStrategyOptions.RaSearchOrder.WestFirst
                    : BlindSearchStrategyOptions.RaSearchOrder.EastFirst,
                SearchOrderDec = options.SouthFirst
                    ? BlindSearchStrategyOptions.DecSearchOrder.SouthFirst
                    : BlindSearchStrategyOptions.DecSearchOrder.NorthFirst
            };

            var strategy = new BlindSearchStrategy(strategyOptions);
            return strategy;
        }

        private static uint? ParseIntermediateFieldRadiusSteps(string input)
        {
            if (input == null)
                return 0;

            if (input == "auto")
                return null;
            
            return uint.Parse(input, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        private static NearbySearchStrategy ParseStrategy(NearbyOptions options)
        {
            EquatorialCoords center = new EquatorialCoords(0, 0);
            var parseErrors = new List<string>();

            NearbySearchStrategy strategy;
            var strategyOptions = new NearbySearchStrategyOptions
            {
                MaxNegativeDensityOffset = options.LowerDensityOffset,
                MaxPositiveDensityOffset = options.HigherDensityOffset,
                UseParallelism = options.UseParallelism,
                SearchAreaRadiusDegrees = options.SearchRadius
            };
            

            if (!File.Exists(options.ImageFilename))
            {
                parseErrors.Add("--image: the file does not exist.");
                ErrorAction(_parserResult, new Error[0], parseErrors);
                Environment.Exit(1);
            }

            if (options.UseManualParams)
            {
                if (options.Ra.Trim().Contains(' '))
                {
                    try
                    {
                        center.Ra = Conversions.RaToDecimal(options.Ra);
                    }
                    catch (Exception e)
                    {
                        parseErrors.Add("--ra: " + e.Message);
                    }
                }
                else
                {
                    if (double.TryParse(options.Ra, NumberStyles.Float, CultureInfo.InvariantCulture, out var ra))
                        center.Ra = ra;
                    else
                    {
                        parseErrors.Add("--ra: invalid number format.");
                    }
                }

                if (options.Dec.Trim().Contains(' '))
                {
                    try
                    {
                        center.Dec = Conversions.DecToDecimal(options.Dec);
                    }
                    catch (Exception e)
                    {
                        parseErrors.Add("--dec: " + e.Message);
                    }
                }
                else
                {
                    if (double.TryParse(options.Dec, NumberStyles.Float, CultureInfo.InvariantCulture, out var dec))
                        center.Dec = dec;
                    else
                    {
                        parseErrors.Add("--dec: invalid number format.");
                    }
                }

                var fieldRadiusMinMaxRegex = new Regex(@"^(\d+\.*\d*)-(\d+\.*\d*)$");
                if (!string.IsNullOrEmpty(options.FieldRadiusMinMax) &&
                    fieldRadiusMinMaxRegex.IsMatch(options.FieldRadiusMinMax))
                {
                    var val1 = double.Parse(fieldRadiusMinMaxRegex.Match(options.FieldRadiusMinMax).Groups[1].Value, 
                        CultureInfo.InvariantCulture);
                    var val2 = double.Parse(fieldRadiusMinMaxRegex.Match(options.FieldRadiusMinMax).Groups[2].Value,
                        CultureInfo.InvariantCulture);
                    strategyOptions.MaxFieldRadiusDegrees = val1 > val2 ? val1 : val2;
                    strategyOptions.MinFieldRadiusDegrees = val1 < val2 ? val1 : val2;

                    strategyOptions.IntermediateFieldRadiusSteps =
                        ParseIntermediateFieldRadiusSteps(options.IntermediateFieldRadiusSteps);
                }
                else
                {
                    strategyOptions.MinFieldRadiusDegrees = options.FieldRadius;
                    strategyOptions.MaxFieldRadiusDegrees = options.FieldRadius;
                }

                strategy = new NearbySearchStrategy(center, strategyOptions);
                return strategy;
            }

            if (options.UseFitsHeaders)
            {
                DefaultFitsReader fitsReader = new DefaultFitsReader();
                try
                {
                    var fitsImage = fitsReader.FromFile(options.ImageFilename);
                    if(fitsImage.Metadata.CenterPos == null)
                        parseErrors.Add("FITS RA, DEC was not available in headers, manual coordinates required.");
                    if(fitsImage.Metadata.ViewSize == null && options.FieldRadius == 0)
                        parseErrors.Add("FITS camera view area was not available in headers, and --field-radius was not given.");

                    if (parseErrors.Any())
                    {
                        ErrorAction(_parserResult, new Error[0], parseErrors);
                        Environment.Exit(1);
                    }

                    center = fitsImage.Metadata.CenterPos;
                    strategyOptions.MaxFieldRadiusDegrees = fitsImage.Metadata.ViewSize != null
                        ? (float)fitsImage.Metadata.ViewSize.DiameterDeg * 0.5f
                        : options.FieldRadius;

                    // Looks like we have what we need.
                    strategy = new NearbySearchStrategy(center, strategyOptions);
                    return strategy;
                }
                catch (Exception e)
                {
                    parseErrors.Add("FITS file parsing failed: " + e.Message);
                    ErrorAction(_parserResult, new Error[0], parseErrors);
                    Environment.Exit(1);
                }
                
            }
            
            // should never reach here.
            return null;
        }


        private static List<string> ValidateGeneric(GenericOptions options)
        {
            var errors = new List<string>();
            if(!new [] {"tsv", "json"}.Contains(options.OutFormat))
                errors.Add("--out-format: must be either json or tsv");

            // Check that we can support this image file type.
            var isSupported = DefaultFitsReader.IsSupported(options.ImageFilename) ||
                              CommonFormatsImageReader.IsSupported(options.ImageFilename);
            if (!isSupported)
            {
                errors.Add("image is not in a supported format");
            }

            _benchmarkMode = options.Benchmark;

            return errors;
        }

        private static void Validate(BlindOptions options)
        {
            var errors = new List<string>();
            errors.AddRange(ValidateGeneric(options));

            if (options.MinRadius <= ConstraintValues.MinRecommendedFieldRadius)
            {
                errors.Add($"--min-radius: value must be > {ConstraintValues.MinRecommendedFieldRadius}");
            }

            if (options.MaxRadius > ConstraintValues.MaxRecommendedFieldRadius)
            {
                errors.Add($"--max-radius: value must be <= {ConstraintValues.MaxRecommendedFieldRadius}");
            }

            if (options.MinRadius > options.MaxRadius)
            {
                errors.Add("--max-radius: value must be larger >= --min-radius");
            }
            
            if (errors.Any())
            {
                ErrorAction(_parserResult, new Error[0], errors);
                Environment.Exit(1);
            }
        }

        private static void Validate(NearbyOptions options)
        {
            // Extra validation is required, since CommandLineParser grouping and required params
            // don't mix well. --ra, --dec and --field-radius should be required if --manual is set.
            // Also a bunch of other validations need to be made...

            var errors = new List<string>();
            errors.AddRange(ValidateGeneric(options));

            var fieldRadiusMinMaxRegex = new Regex(@"^(\d+\.*\d*)-(\d+\.*\d*)$");
            var fieldRadiusStepsRegex = new Regex(@"^(auto|\d+)$");

            if (options.UseManualParams)
            {
                if (string.IsNullOrEmpty(options.Ra)) errors.Add("--ra: parameter was not provided.");
                if (string.IsNullOrEmpty(options.Dec)) errors.Add("--dec: parameter was not provided.");

                var hasValidMinMax = !string.IsNullOrEmpty(options.FieldRadiusMinMax) &&
                    fieldRadiusMinMaxRegex.IsMatch(options.FieldRadiusMinMax);

                if (!string.IsNullOrEmpty(options.IntermediateFieldRadiusSteps) && !fieldRadiusStepsRegex.IsMatch(options.IntermediateFieldRadiusSteps))
                {
                    errors.Add("--field-radius-steps: invalid value, valid values are: a number or string 'auto'");
                }

                if (options.FieldRadius <= 0 && !hasValidMinMax) errors.Add("--field-radius or --field-radius-range: was not provided.");

                // Validate that min-max is within recommended range.
                if (hasValidMinMax)
                {
                    var val1 = double.Parse(fieldRadiusMinMaxRegex.Match(options.FieldRadiusMinMax).Groups[1].Value,
                        CultureInfo.InvariantCulture);
                    var val2 = double.Parse(fieldRadiusMinMaxRegex.Match(options.FieldRadiusMinMax).Groups[2].Value,
                        CultureInfo.InvariantCulture);
                    var max = val1 > val2 ? val1 : val2;
                    var min = val1 < val2 ? val1 : val2;
                    if(max > ConstraintValues.MaxRecommendedFieldRadius)
                        errors.Add($"--field-radius-range: value over max recommended radius value of {ConstraintValues.MaxRecommendedFieldRadius}");
                    if (min < ConstraintValues.MinRecommendedFieldRadius)
                        errors.Add($"--field-radius-range: value less than min recommended radius value of {ConstraintValues.MinRecommendedFieldRadius}");
                }

                if (errors.Any())
                    errors.Insert(0, "Manual input flag was selected, but:");
            }
            else
            {
                if (!options.UseFitsHeaders) errors.Add("either --use-fits-headers or --manual flag should be selected.");
            }

            if (errors.Any())
            {
                ErrorAction(_parserResult, new Error[0], errors);
                Environment.Exit(1);
            }
        }

        

        private static void ErrorAction(ParserResult<object> result, IEnumerable<Error> errors, List<string> customErrors)
        {
            var appVersion = typeof(Program).Assembly.GetName().Version.ToString(3);

            if (errors.Any(e => e.Tag == ErrorType.VersionRequestedError))
            {
                Console.WriteLine($"{ApplicationName} {appVersion}");
                Environment.Exit(0);
            }
            
            var optionsType = result.TypeInfo.Current;

            var ht = HelpText.AutoBuild(result, helpText =>
            {
                return HelpText.DefaultParsingErrorsHandler(result, helpText);
            }, example =>
            {
                return example;
            });

            ht.Heading = $"{ApplicationName} {appVersion}";
            ht.AddNewLineBetweenHelpSections = true;
            if (optionsType == typeof(BlindOptions))
                ht.OptionComparison = BlindOptionComparison;
            if (optionsType == typeof(NearbyOptions))
                ht.OptionComparison = NearbyOptionComparison;

            if (customErrors != null && customErrors.Any())
            {
                // This is a little bit dirty, but there's no slot for text between copyright and usage.
                ht.Copyright = ht.Copyright + "\n" +
                    string.Join("\n", customErrors.Select(x => $"  {x}").Prepend("\nERROR(S):"));
            }
            Console.WriteLine(ht);

            
        }

        // Just for ordering the parameters
        private static int NearbyOptionComparison(ComparableOption x, ComparableOption y)
        {
            var nearbyOrder = new List<string>
            {
                "use-config", "image", "out", "out-format", "manual", "use-fits-headers", "ra", "dec", "field-radius", 
                "field-radius-range", "field-radius-steps", "search-radius",
                "lower-density-offset", "higher-density-offset", "use-parallelism"
            };

            var opt1Index = nearbyOrder.IndexOf(x.LongName);
            var opt2Index = nearbyOrder.IndexOf(y.LongName);

            return opt1Index < opt2Index ? -1 : 1;

        }

        // Just for ordering the parameters
        private static int BlindOptionComparison(ComparableOption x, ComparableOption y)
        {
            var blindOrder = new List<string>
            {
                "use-config", "image", "out", "out-format", "min-radius", "max-radius", "lower-density-offset", "higher-density-offset", "east-first",
                "west-first", "north-first", "south-first", "use-parallelism"
            };

            var opt1Index = blindOrder.IndexOf(x.LongName);
            var opt2Index = blindOrder.IndexOf(y.LongName);

            return opt1Index < opt2Index ? -1 : 1;
        }
    }
}
