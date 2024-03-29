﻿// Copyright (c) Jussi Saarivirta.
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
using WatneyAstrometry.Core.Exceptions;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.Image;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.QuadDb;
using WatneyAstrometry.Core.StarDetection;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.ImageReaders;
using WatneyAstrometry.SolverApp.Exceptions;

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

        private static Stream _stdinStream;
        private static XyList _xyList;


        internal enum InputType
        {
            Unknown,
            FitsFromFile,
            FitsFromStdin,
            CommonImageFromFile,
            CommonImageFromStdin,
            Xyls
        }

        public static void Main(string[] args)
        {
            // Enforce this, otherwise help texts will vary depending on culture and we don't want that.
            // Is this a bug in the command line parser 2.8?
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            using var imageMemStream = new MemoryStream();

            // If we have an input stream, read it.
            if (args.Contains("--xyls-stdin") || args.Contains("--image-stdin"))
            {
                using (var inputStream = Console.OpenStandardInput())
                {
                    var buf = new byte[4096];
                    int read = 0;
                    while((read = inputStream.Read(buf, 0, 4096)) > 0)
                        imageMemStream.Write(buf, 0, read);

                    imageMemStream.Seek(0, SeekOrigin.Begin);
                    //inputStream.CopyToAsync(imageMemStream);
                    _stdinStream = imageMemStream;
                }
            }

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
                Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

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
            else
            {
                _verboseLogger = new NullVerboseLogger();
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

        private static IImage GetImageFromStdin(GenericOptions options, bool isFits)
        {
            var commonFormatsImageReader = new CommonFormatsImageReader();
            var defaultFitsReader = new DefaultFitsReader();

            IImage image = null;
            if (options.ImageFromStdin && _stdinStream != null)
            {
                image = isFits 
                    ? defaultFitsReader.FromStream(_stdinStream) 
                    : commonFormatsImageReader.FromStream(_stdinStream);
            }

            return image;
        }

        private static void RunSolve(ISearchStrategy strategy, GenericOptions options, InputType inputType)
        {
            var quadDatabase = new CompactQuadDatabase();
            var starDetector = _configuration.DefaultStarDetectionBgOffset != null
                ? new DefaultStarDetector(_configuration.DefaultStarDetectionBgOffset.Value)
                : new DefaultStarDetector();
            
            var solver = new Solver(_verboseLogger)
                .UseStarDetector(starDetector)
                .UseImageReader<CommonFormatsImageReader>(() => new CommonFormatsImageReader(), CommonFormatsImageReader.SupportedImageExtensions)
                .UseQuadDatabase(() => quadDatabase.UseDataSource(_configuration.QuadDatabasePath));
            solver.OnSolveProgress += BenchmarkProgressHandler;

            var globalSolverConfiguration = Solver.SolverGlobalConfiguration;
            if (options.LimitThreads > 0)
                globalSolverConfiguration.MaxThreads = options.LimitThreads;
            
            Solver.SetGlobalConfiguration(globalSolverConfiguration);
            
            var solverOptions = ParseSolverOptions(options);
            _verboseLogger.WriteInfo($"System is little endian: {BitConverter.IsLittleEndian}");

            var solveTask = Task.Run(async () =>
            {
                try
                {
                    if (inputType is InputType.CommonImageFromFile or InputType.FitsFromFile)
                    {
                        _verboseLogger.WriteInfo($"Solver input is file, {options.ImageFilename}");
                        return await solver.SolveFieldAsync(options.ImageFilename, strategy, solverOptions,
                            CancellationToken.None);
                    }

                    if (inputType == InputType.CommonImageFromStdin)
                    {
                        _verboseLogger.WriteInfo("Solver input is common image format read from stdin");
                        var commonFormatsImageReader = new CommonFormatsImageReader();
                        var image = commonFormatsImageReader.FromStream(_stdinStream);
                        return await solver.SolveFieldAsync(image, strategy, solverOptions, CancellationToken.None);
                    }

                    if (inputType == InputType.FitsFromStdin)
                    {
                        _verboseLogger.WriteInfo("Solver input is FITS read from stdin");
                        var defaultFitsReader = new DefaultFitsReader();
                        var image = defaultFitsReader.FromStream(_stdinStream);
                        return await solver.SolveFieldAsync(image, strategy, solverOptions, CancellationToken.None);
                    }

                    if (inputType == InputType.Xyls)
                    {
                        _verboseLogger.WriteInfo("Solver input is XYLS");
                        return await solver.SolveFieldAsync(_xyList, _xyList.Stars, strategy, solverOptions,
                            CancellationToken.None);
                    }

                    return null;
                }
                catch (Exception e)
                {
                    LogException(e);
                    _verboseLogger.Flush();
                    throw;
                }
                
            });
            solveTask.Wait();

            quadDatabase.Dispose();

            var result = solveTask.Result;
            SaveOutput(result, options, options.ExtendedOutput);
            _verboseLogger.Flush();
        }

        private static void RunBlindSolve(BlindOptions options)
        {
            try
            {
                GetLogger(options);
                LoadConfiguration(options);
                AddDefaultParametersFromConfig(options);
                Validate(options, out InputType inputType);

                var strategy = ParseStrategy(options);
                RunSolve(strategy, options, inputType);
            }
            catch (Exception e)
            {
                LogException(e);
                _verboseLogger.Flush();
                Environment.Exit(1);
            }
        }

        private static void RunNearbySolve(NearbyOptions options)
        {
            try
            {
                GetLogger(options);
                LoadConfiguration(options);
                AddDefaultParametersFromConfig(options);
                Validate(options, out InputType inputType);

                var strategy = ParseStrategy(options);
                RunSolve(strategy, options, inputType);
            }
            catch (Exception e)
            {
                LogException(e);
                _verboseLogger.Flush();
                Environment.Exit(1);
            }
        }

        private static void WriteConsoleError(string message) => Console.WriteLine($"WATNEY ERROR: {message}");
        

        private static void LogException(Exception e)
        {
            if (e is ConfigException configException)
            {
                _verboseLogger.WriteError($"Configuration problem. {configException.Message}");
                WriteConsoleError($"Configuration problem. {configException.Message}");
            }
            else if (e is SolverException solverException)
            {
                _verboseLogger.WriteError($"Solver problem. {solverException.Message}");
                WriteConsoleError($"Solver problem. {solverException.Message}");
            }
            else if (e is SolverInputException solverInputException)
            {
                _verboseLogger.WriteError($"Problem with solver input. {solverInputException.Message}");
                WriteConsoleError($"Problem with solver input. {solverInputException.Message}");
            }
            else if (e is QuadDatabaseException quadDatabaseException)
            {
                _verboseLogger.WriteError($"Quad database problem. {quadDatabaseException.Message}");
                WriteConsoleError($"Quad database problem. {quadDatabaseException.Message}");
            }
            else if (e is QuadDatabaseVersionException quadDatabaseVersionException)
            {
                _verboseLogger.WriteError($"Quad database version issue. {quadDatabaseVersionException.Message}");
                WriteConsoleError($"Quad database version issue. {quadDatabaseVersionException.Message}");
            }
            else
            {
                _verboseLogger.WriteError($"Uncaught program exception: {e.Message}");
                _verboseLogger.WriteError($"Exception stack trace: {e.StackTrace}");
                WriteConsoleError($"Uncaught program exception: {e.Message}");
                WriteConsoleError($"Exception stack trace: {e.StackTrace}");
            }
            
        }

        // Read defaults from config, and apply them to arguments that were not
        // explicitly set.
        private static void AddDefaultParametersFromConfig(NearbyOptions options)
        {
            AddDefaultGenericParametersFromConfig(options);

            if (options.SearchRadius == null)
            {
                if (_configuration.DefaultNearbySearchRadius != null)
                    options.SearchRadius = _configuration.DefaultNearbySearchRadius;
                else
                    options.SearchRadius = 10; // Hardcoded default, if nothing else is provided.
            }
            
            if (options.Sampling == 0)
            {
                if (_configuration.DefaultNearbySampling != null)
                    options.Sampling = (int)_configuration.DefaultNearbySampling.Value;
            }

            if (options.UseParallelism == null)
            {
                if (_configuration.DefaultNearbyParallelism != null)
                    options.UseParallelism = _configuration.DefaultNearbyParallelism;
                else
                    options.UseParallelism = false; // Hardcoded default, if nothing else is provided.
            }

        }

        // Read defaults from config, and apply them to arguments that were not
        // explicitly set.
        private static void AddDefaultParametersFromConfig(BlindOptions options)
        {
            AddDefaultGenericParametersFromConfig(options);

            if (options.MaxRadius == null)
            {
                if (_configuration.DefaultBlindMaxRadius != null)
                    options.MaxRadius = _configuration.DefaultBlindMaxRadius;
                else
                    options.MaxRadius = 8.0; // Hardcoded default, if nothing else is provided.
            }

            if (options.MinRadius == null)
            {
                if (_configuration.DefaultBlindMinRadius != null)
                    options.MinRadius = _configuration.DefaultBlindMinRadius;
                else
                    options.MinRadius = 0.5; // Hardcoded default, if nothing else is provided.
            }

            if (options.Sampling == 0)
            {
                if(_configuration.DefaultBlindSampling != null)
                    options.Sampling = (int)_configuration.DefaultBlindSampling.Value;
            }

            if (options.UseParallelism == null)
            {
                if (_configuration.DefaultBlindParallelism != null)
                    options.UseParallelism = _configuration.DefaultBlindParallelism;
                else
                    options.UseParallelism = true; // Hardcoded default, if nothing else is provided.
            }

        }

        private static void AddDefaultGenericParametersFromConfig(GenericOptions options)
        {

            if (options.LowerDensityOffset == null)
            {
                if (_configuration.DefaultLowerDensityOffset != null)
                    options.LowerDensityOffset = _configuration.DefaultLowerDensityOffset;
                else
                    options.LowerDensityOffset = 1; // Hardcoded default, if nothing else is provided.
            }

            if (options.HigherDensityOffset == null)
            {
                if (_configuration.DefaultHigherDensityOffset != null)
                    options.HigherDensityOffset = _configuration.DefaultHigherDensityOffset;
                else
                    options.HigherDensityOffset = 1; // Hardcoded default, if nothing else is provided.
            }

            if (options.MaxStars == 0)
            {
                if (_configuration.DefaultMaxStars != null)
                    options.MaxStars = (int)_configuration.DefaultMaxStars.Value;
            }

            if (options.LimitThreads == 0)
            {
                if (_configuration.DefaultLimitThreads != null)
                    options.LimitThreads = _configuration.DefaultLimitThreads.Value;
            }
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
                outputData.Add("ra_hms", Conversions.RaDegreesToHhMmSs(result.Solution.PlateCenter.Ra));
                outputData.Add("dec_dms", Conversions.DecDegreesToDdMmSs(result.Solution.PlateCenter.Dec));
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
                _verboseLogger.WriteInfo("== Solution ==");
                foreach (var item in outputData)
                {
                    _verboseLogger.WriteInfo($"* {item.Key}: {item.Value}");
                }
            }

            if (result.Success && !string.IsNullOrEmpty(options.WcsFile))
            {
                _verboseLogger.WriteInfo($"Writing WCS file to {options.WcsFile}");
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
                MaxNegativeDensityOffset = options.LowerDensityOffset.Value,
                MaxPositiveDensityOffset = options.HigherDensityOffset.Value,
                UseParallelism = options.UseParallelism.Value,
                MinRadiusDegrees = options.MinRadius.Value,
                StartRadiusDegrees = options.MaxRadius.Value,
                SearchOrderRa = options.WestFirst
                    ? BlindSearchStrategyOptions.RaSearchOrder.WestFirst
                    : BlindSearchStrategyOptions.RaSearchOrder.EastFirst,
                SearchOrderDec = options.SouthFirst
                    ? BlindSearchStrategyOptions.DecSearchOrder.SouthFirst
                    : BlindSearchStrategyOptions.DecSearchOrder.NorthFirst
            };

            _verboseLogger.WriteInfo("Blind search strategy");
            _verboseLogger.WriteInfo($"- MaxNegativeDensityOffset: {options.LowerDensityOffset}");
            _verboseLogger.WriteInfo($"- MaxPositiveDensityOffset: {options.HigherDensityOffset}");
            _verboseLogger.WriteInfo($"- UseParallelism: {options.UseParallelism}");
            _verboseLogger.WriteInfo($"- StartRadiusDegrees: {options.MaxRadius}");
            _verboseLogger.WriteInfo($"- MinRadiusDegrees: {options.MinRadius}");
            
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
                MaxNegativeDensityOffset = options.LowerDensityOffset.Value,
                MaxPositiveDensityOffset = options.HigherDensityOffset.Value,
                UseParallelism = options.UseParallelism.Value,
                SearchAreaRadiusDegrees = options.SearchRadius.Value
            };

            // For convenience
            void LogStrategyOpts()
            {
                _verboseLogger.WriteInfo("Nearby search strategy");
                _verboseLogger.WriteInfo($"- MaxNegativeDensityOffset: {strategyOptions.MaxNegativeDensityOffset}");
                _verboseLogger.WriteInfo($"- MaxPositiveDensityOffset: {strategyOptions.MaxPositiveDensityOffset}");
                _verboseLogger.WriteInfo($"- UseParallelism: {strategyOptions.UseParallelism}");
                _verboseLogger.WriteInfo($"- SearchAreaRadiusDegrees: {strategyOptions.SearchAreaRadiusDegrees}");
                _verboseLogger.WriteInfo($"- MinFieldRadiusDegrees: {strategyOptions.MinFieldRadiusDegrees}");
                _verboseLogger.WriteInfo($"- MaxFieldRadiusDegrees: {strategyOptions.MaxFieldRadiusDegrees}");
                _verboseLogger.WriteInfo($"- IntermediateFieldRadiusSteps: {strategyOptions.IntermediateFieldRadiusSteps}");
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

                if (parseErrors.Any())
                {
                    _verboseLogger.WriteError("Late validation produced errors.");
                    parseErrors.ForEach(err => _verboseLogger.WriteError(err));
                    ErrorAction(_parserResult, new Error[0], parseErrors);
                    Environment.Exit(1);
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
                LogStrategyOpts();
                _verboseLogger.WriteInfo($"Search center: {center}");
                return strategy;
            }

            if (options.UseFitsHeaders)
            {
                DefaultFitsReader fitsReader = new DefaultFitsReader();
                try
                {
                    
                    IImage fitsImage = null;
                    if (options.ImageFromStdin)
                        fitsImage = fitsReader.FromStream(_stdinStream);
                    else
                        fitsImage = fitsReader.FromFile(options.ImageFilename);

                    if(fitsImage.Metadata.CenterPos == null)
                        parseErrors.Add("FITS RA, DEC was not available in headers, manual coordinates required.");
                    if(fitsImage.Metadata.ViewSize == null && options.FieldRadius == 0)
                        parseErrors.Add("FITS camera view area was not available in headers, and --field-radius was not given.");

                    if (parseErrors.Any())
                    {
                        _verboseLogger.WriteError("Late validation produced errors.");
                        parseErrors.ForEach(err => _verboseLogger.WriteError(err));
                        ErrorAction(_parserResult, new Error[0], parseErrors);
                        _verboseLogger.Flush();
                        Environment.Exit(1);
                    }

                    center = fitsImage.Metadata.CenterPos;
                    strategyOptions.MaxFieldRadiusDegrees = fitsImage.Metadata.ViewSize != null
                        ? (float)fitsImage.Metadata.ViewSize.DiameterDeg * 0.5f
                        : options.FieldRadius;
                    strategyOptions.MinFieldRadiusDegrees = strategyOptions.MaxFieldRadiusDegrees;

                    // Looks like we have what we need.
                    strategy = new NearbySearchStrategy(center, strategyOptions);
                    LogStrategyOpts();
                    _verboseLogger.WriteInfo($"Search center: {center}");
                    return strategy;
                }
                catch (Exception e)
                {
                    var errorMessage = "FITS file parsing failed: " + e.Message;
                    parseErrors.Add(errorMessage);
                    _verboseLogger.WriteError("Late validation produced errors.");
                    parseErrors.ForEach(err => _verboseLogger.WriteError(err));
                    WriteConsoleError(errorMessage);
                    _verboseLogger.Flush();
                    //ErrorAction(_parserResult, new Error[0], parseErrors);
                    Environment.Exit(1);
                }
                
            }
            
            // should never reach here.
            return null;
        }

        private static (int w, int h) ParseXylsImageSize(string s)
        {
            if (s == null)
                return default;

            var regex = new Regex(@"^(\d+)x(\d+)$");
            var match = regex.Match(s);
            if (!match.Success)
                return default;

            var w = int.Parse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            var h = int.Parse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);

            if (w == 0 || h == 0)
                return default;

            return (w, h);
        }

        private static List<string> ValidateGeneric(GenericOptions options, out InputType inputType)
        {
            var errors = new List<string>();
            if(!new [] {"tsv", "json"}.Contains(options.OutFormat))
                errors.Add("--out-format: must be either json or tsv");

            inputType = InputType.Unknown;

            var imageSize = ParseXylsImageSize(options.XylsImageSize);

            if (options.XylsFromStdin || !string.IsNullOrEmpty(options.XylsFilename))
            {
                if (imageSize == default)
                    errors.Add("a valid size in --xyls-imagesize must be provided");
            }

            if (options.ImageFromStdin || options.XylsFromStdin)
            {
                if (_stdinStream == null)
                {
                    errors.Add("image expected from stdin but not received");
                    return errors;
                }
            }

            if (options.ImageFromStdin)
            {
                var isFits = DefaultFitsReader.IsSupported(_stdinStream);
                if (isFits)
                    inputType = InputType.FitsFromStdin;
                else if (CommonFormatsImageReader.IsSupported(_stdinStream))
                    inputType = InputType.CommonImageFromStdin;
                else
                    errors.Add("image is not in a supported format");
            }
            else if (options.XylsFromStdin)
            {
                _xyList = XyList.FromStream(_stdinStream);
                if (_xyList == null)
                {
                    errors.Add("could not read x,y list, the format was not the " +
                        "expected FITS with binary extension and x,y,mag list of floats.");
                    return errors;
                }

                inputType = InputType.Xyls;

                _xyList.ImageHeight = imageSize.h;
                _xyList.ImageWidth = imageSize.w;
            }
            else
            {
                if (string.IsNullOrEmpty(options.ImageFilename) && string.IsNullOrEmpty(options.XylsFilename))
                {
                    errors.Add("image or xyls filename was not supplied");
                }
                if (!string.IsNullOrEmpty(options.ImageFilename))
                {
                    var isFits = DefaultFitsReader.IsSupported(options.ImageFilename);
                    if (isFits)
                        inputType = InputType.FitsFromFile;
                    else if (CommonFormatsImageReader.IsSupported(options.ImageFilename))
                        inputType = InputType.CommonImageFromFile;

                    if(inputType == InputType.Unknown)
                        errors.Add("image is not in a supported format");
                }

                if (!string.IsNullOrEmpty(options.XylsFilename))
                {
                    using (var stream = File.OpenRead(options.XylsFilename))
                    {
                        _xyList = XyList.FromStream(stream);
                    }

                    if (_xyList == null)
                    {
                        errors.Add("could not read x,y list, the format was not the " +
                                   "expected FITS with binary extension and x,y,mag list of floats.");
                        return errors;
                    }

                    inputType = InputType.Xyls;

                    _xyList.ImageHeight = imageSize.h;
                    _xyList.ImageWidth = imageSize.w;
                }
            }
            
            _benchmarkMode = options.Benchmark;

            return errors;
        }

        private static void Validate(BlindOptions options, out InputType inputType)
        {
            var errors = new List<string>();
            errors.AddRange(ValidateGeneric(options, out inputType));

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
                _verboseLogger.WriteError("Late argument validation produced errors.");
                errors.ForEach(err => _verboseLogger.WriteError(err));
                ErrorAction(_parserResult, new Error[0], errors);
                _verboseLogger.Flush();
                Environment.Exit(1);
            }
        }

        private static void Validate(NearbyOptions options, out InputType inputType)
        {
            // Extra validation is required, since CommandLineParser grouping and required params
            // don't mix well. --ra, --dec and --field-radius should be required if --manual is set.
            // Also a bunch of other validations need to be made...

            var errors = new List<string>();
            errors.AddRange(ValidateGeneric(options, out inputType));

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
                // No image; xyls, so we don't have headers to work with!
                if (!options.ImageFromStdin && string.IsNullOrEmpty(options.ImageFilename))
                {
                    errors.Add("cannot use --use-fits-headers when operating with X,Y list.");
                }

                if (!options.UseFitsHeaders) 
                    errors.Add("either --use-fits-headers or --manual flag should be selected.");
            }

            if (errors.Any())
            {
                _verboseLogger.WriteError("Late argument validation produced errors.");
                errors.ForEach(err => _verboseLogger.WriteError(err));
                ErrorAction(_parserResult, new Error[0], errors);
                _verboseLogger.Flush();
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
