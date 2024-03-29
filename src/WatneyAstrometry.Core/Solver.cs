﻿// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Exceptions;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.Image;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.QuadDb;
using WatneyAstrometry.Core.StarDetection;
using WatneyAstrometry.Core.Threading;
using WatneyAstrometry.Core.Types;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// A star quad based plate solver that runs the star detection algorithm on the image to extract stars, then
    /// forms star quads from them and runs the comparison algorithm to find matches from a star quad catalog.
    /// </summary>
    public class Solver : ISolver
    {
        
        
        private Dictionary<string, (Type type, Func<IImageReader> factory)> _imageReaderFactories = 
            new Dictionary<string, (Type, Func<IImageReader>)>(StringComparer.InvariantCultureIgnoreCase);

        private Func<IStarDetector> _starDetectorFactory = () => new DefaultStarDetector();
        private Func<Task<IStarDetector>> _starDetectorFactoryAsync = null;
        private Func<IQuadDatabase> _quadDatabaseFactory = () => (IQuadDatabase) null;
        private Func<Task<IQuadDatabase>> _quadDatabaseFactoryAsync = null;
        
        private int _tentativeMatches = 0;
        private int _iterations = 0;
        private Guid _solveContextId;

        private IVerboseLogger _logger;

        /// <summary>
        /// A progress handler delegate. Invoked when the solver process progresses.
        /// </summary>
        /// <param name="step"></param>
        public delegate void SolveProgressHandler(SolverStep step);
        /// <summary>
        /// Event that is triggered as we enter different steps in the solving process.
        /// </summary>
        public event SolveProgressHandler OnSolveProgress;

        /// <summary>
        /// New solver instance.
        /// </summary>
        /// <param name="logger"></param>
        public Solver(IVerboseLogger logger = null)
        {
            _logger = logger ?? new NullVerboseLogger();
            UseImageReader<DefaultFitsReader>(() => new DefaultFitsReader(), "fit", "fits");
        }

        /// <summary>
        /// Global Solver configuration settings.
        /// </summary>
        public static SolverGlobalConfiguration SolverGlobalConfiguration { get; private set; } = SolverGlobalConfiguration.Default;

        /// <summary>
        /// Sets the global configuration.
        /// </summary>
        /// <param name="config"></param>
        public static void SetGlobalConfiguration(SolverGlobalConfiguration config)
        {
            SolverGlobalConfiguration = config;
            WatneyTaskFactory.SetConcurrentTasks(config.MaxThreads);
        }

        /// <summary>
        /// Specify an image reader for the solver that can be used to read different kinds of images.
        /// </summary>
        /// <typeparam name="T">The type of the image reader</typeparam>
        /// <param name="factoryFunc">A factory function that produces a new instance of the reader</param>
        /// <param name="fileExtensions">Which file extensions this reader can handle (file extensions without the dot)</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Solver UseImageReader<T>(Func<IImageReader> factoryFunc, params string[] fileExtensions) where T: IImageReader
        {
            var type = typeof(T);
            if (fileExtensions == null || fileExtensions.Length == 0)
                throw new Exception("Image reader must have one or more file extensions assigned to it");
            foreach (var ext in fileExtensions)
            {
                _imageReaderFactories.Add(ext, (type, factoryFunc));
            }
            return this;
        }

        /// <summary>
        /// Removes an image reader registration from the solver.
        /// </summary>
        /// <typeparam name="T">The type of the registered image reader</typeparam>
        /// <returns></returns>
        public Solver RemoveImageReader<T>() where T : IImageReader
        {
            var type = typeof(T);
            var itemsToRemove = _imageReaderFactories.Where(kvp => kvp.Value.type == type)
                .Select(x => x.Key)
                .ToList();
            itemsToRemove.ForEach(x => _imageReaderFactories.Remove(x));
            return this;
        }

        /// <summary>
        /// Clears the full list of registered image readers.
        /// </summary>
        /// <returns></returns>
        public Solver ClearImageReaders()
        {
            _imageReaderFactories.Clear();
            return this;
        }

        /// <summary>
        /// Use a specified star detector instance for star detection.
        /// </summary>
        /// <param name="starDetector"></param>
        /// <returns></returns>
        public Solver UseStarDetector(IStarDetector starDetector)
        {
            _starDetectorFactory = () => starDetector;
            return this;
        }

        /// <summary>
        /// Use a specified star detector instance factory method for star detection.
        /// </summary>
        /// <param name="factoryFunc"></param>
        /// <returns></returns>
        public Solver UseStarDetector(Func<IStarDetector> factoryFunc)
        {
            _starDetectorFactory = factoryFunc;
            return this;
        }

        /// <summary>
        /// Use a specified star detector instance factory method (async) for star detection.
        /// </summary>
        /// <param name="asyncFactoryFunc"></param>
        /// <returns></returns>
        public Solver UseStarDetector(Func<Task<IStarDetector>> asyncFactoryFunc)
        {
            _starDetectorFactoryAsync = asyncFactoryFunc;
            return this;
        }

        /// <summary>
        /// Use a specified quad database factory method for instantiating a quad database.
        /// </summary>
        /// <param name="factoryFunc"></param>
        /// <returns></returns>
        public Solver UseQuadDatabase(Func<IQuadDatabase> factoryFunc)
        {
            _quadDatabaseFactory = factoryFunc;
            return this;
        }

        /// <summary>
        /// Use a specified quad database instance.
        /// </summary>
        /// <param name="quadDatabase"></param>
        /// <returns></returns>
        public Solver UseQuadDatabase(IQuadDatabase quadDatabase)
        {
            _quadDatabaseFactory = () => quadDatabase;
            return this;
        }

        /// <summary>
        /// Use a specified quad database factory method (async) for instantiating a quad database.
        /// </summary>
        /// <param name="asyncFactoryFunc"></param>
        /// <returns></returns>
        public Solver UseQuadDatabase(Func<Task<IQuadDatabase>> asyncFactoryFunc)
        {
            _quadDatabaseFactoryAsync = asyncFactoryFunc;
            return this;
        }

        /// <inheritdoc />
        public async Task<SolveResult> SolveFieldAsync(string filename, ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken)
        {
            _logger.WriteInfo($"Solving field from file {filename}, with strategy {strategy.GetType().Name}");
            var filenameExtension = Path.GetExtension(filename);
            if(string.IsNullOrEmpty(filenameExtension))
                throw new SolverInputException("File does not have extension, unable to determine file type");

            filenameExtension = filenameExtension.Replace(".", string.Empty);
            if(!_imageReaderFactories.ContainsKey(filenameExtension))
                throw new SolverInputException($"No ImageReader for file extension '{filenameExtension}' was found, unable to process image");

            OnSolveProgress?.Invoke(SolverStep.ImageReadStarted);

            var reader = _imageReaderFactories[filenameExtension].factory.Invoke();
            var image = reader.FromFile(filename);

            if(image.Metadata?.ViewSize != null)
                _logger.WriteInfo($"Image field radius: {0.5 * image.Metadata.ViewSize.DiameterDeg}");

            if(image.Metadata?.CenterPos != null)
                _logger.WriteInfo($"Image center coordinate: {image.Metadata.CenterPos.ToStringRounded(3)}");

            OnSolveProgress?.Invoke(SolverStep.ImageReadFinished);

            return await SolveFieldAsync(image, strategy, options, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<SolveResult> SolveFieldAsync(IImage image, ISearchStrategy strategy, SolverOptions options,
            CancellationToken cancellationToken)
        {
            // Use the registered star detector.
            IStarDetector starDetector;
            if (_starDetectorFactoryAsync != null)
                starDetector = await _starDetectorFactoryAsync.Invoke();
            else
                starDetector = _starDetectorFactory.Invoke();

            OnSolveProgress?.Invoke(SolverStep.StarDetectionStarted);

            var detectedStars = starDetector.DetectStars(image);
            _logger.WriteInfo($"Detected {detectedStars.Count} stars from the image");

            OnSolveProgress?.Invoke(SolverStep.StarDetectionFinished);

            if (detectedStars.Count == 0)
                return new SolveResult()
                {
                    Success = false,
                    AreasSearched = 0,
                    DiagnosticsData = new SolveDiagnosticsData()
                    {
                        DetectedQuadDensity = 0,
                        DetectedStars = detectedStars,
                        ImageWidth = image.Metadata.ImageWidth,
                        ImageHeight = image.Metadata.ImageHeight
                    }
                };

            return await SolveFieldAsync(image.Metadata, detectedStars, strategy, options, cancellationToken);
        }

        private static List<ImageStar> TakeBrightest(IList<ImageStar> detectedStars, int numStars)
        {
            var starSizes = detectedStars.Select(x => x.StarSize).ToArray();
            Array.Sort(starSizes);
            var midIndex = starSizes.Length / 2;
            var medianSize = starSizes.Length % 2 != 0
                ? starSizes[midIndex]
                : (starSizes[midIndex] + starSizes[midIndex + 1]) / 2;
            return detectedStars.OrderByDescending(star => star.StarSize <= medianSize
                ? star.Brightness
                : star.Brightness + (star.StarSize / medianSize) * 50)
                .Take(numStars)
                .ToList();
        }


        /// <inheritdoc />
        public async Task<SolveResult> SolveFieldAsync(IImageDimensions imageDimensions, IList<ImageStar> stars, 
            ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken)
        {

            if (strategy == null)
                throw new SolverException("Must define a search strategy");

            SolveResult result = null;
            
            _logger.WriteInfo("Image parsed, starting the solve");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            OnSolveProgress?.Invoke(SolverStep.SolveProcessStarted);
            
            // Max stars:
            // Take minimum of 300 stars. 
            // Take maximum of 800 stars.
            // If 0.33 * detected stars > 300, take that many.
            // With really high star density images we may have to go with a large number of stars
            // in order to get a result. Sometimes however, even a 100 star limit works - all depends
            // on the image, the saturation of stars and the size difference of bright stars.

            int maxStars;
            if (options?.UseMaxStars != null)
            {
                maxStars = options.UseMaxStars.Value;
                if (maxStars > ConstraintValues.MaxRecommendedStars)
                {
                    _logger.WriteWarn($"Max stars parameter over the recommended value " +
                        $"of {ConstraintValues.MaxRecommendedStars}, this may cause slow solves");
                }
            }
            else
                maxStars = 0.33 * stars.Count <= 300
                    ? 300 // at least 300 stars
                    : (int)Math.Min(0.33 * stars.Count, 1000);
            
            // No stars? No game.
            if (stars.Count == 0)
            {
                _logger.WriteError("Found 0 stars from the input");
                return new SolveResult()
                {
                    Success = false,
                    AreasSearched = 0,
                    DiagnosticsData = new SolveDiagnosticsData()
                    {
                        DetectedQuadDensity = 0,
                        DetectedStars = stars,
                        ImageWidth = imageDimensions.ImageWidth,
                        ImageHeight = imageDimensions.ImageHeight
                    }
                };
            }

            var chosenDetectedStars = TakeBrightest(stars, maxStars);

            _logger.WriteInfo($"Chose {chosenDetectedStars.Count} stars from the detected stars for quad formation");

            // Note: this can take time if the factory method is actually reading the database files
            // at this moment, or not if the quad database has already been initialized.
            _logger.WriteInfo($"Initializing quad database");
            IQuadDatabase quadDb;
            if (_quadDatabaseFactoryAsync != null)
                quadDb = await _quadDatabaseFactoryAsync.Invoke();
            else
                quadDb = _quadDatabaseFactory.Invoke();

            _logger.WriteInfo($"Quad database is ready");

            _tentativeMatches = 0;
            _iterations = 0;

            _solveContextId = Guid.NewGuid();
            quadDb.CreateSolveContext(_solveContextId);

            OnSolveProgress?.Invoke(SolverStep.QuadFormationStarted);

            // Form quads from image stars
            var (imageStarQuads, countInFirstPass) = FormImageStarQuads(chosenDetectedStars.ToList()); 
            _logger.WriteInfo($"Formed {imageStarQuads.Length} quads from the chosen stars");

            OnSolveProgress?.Invoke(SolverStep.QuadFormationFinished);

            var completionCts = new CancellationTokenSource();
            var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(completionCts.Token, cancellationToken);

            int? sampling = options.UseSampling;

            if(sampling != null && sampling >= 1)
                _logger.WriteInfo($"Using given sampling value, 1/{sampling} quads will be used for the first search round");
            else
            {
                _logger.WriteInfo("Using auto-sampling");
                sampling = 4; // After testing, seems like a good performant value overall.
            }

            var diagnosticsData = new SolveDiagnosticsData()
            {
                DetectedStars = stars,
                DetectedQuadDensity = countInFirstPass,
                MatchInstances = null,
                UsedStarCount = chosenDetectedStars.Count,
                FormedImageStarQuads = imageStarQuads,
                ImageWidth = imageDimensions.ImageWidth,
                ImageHeight = imageDimensions.ImageHeight,
            };

            diagnosticsData.FoundUsingRunType = SolveRunType.SampledRun;

            int numSubSets = sampling.Value;
            int currentSubSetIndex = 0;
            SolveResult successfulSolveResult = null;
            IEnumerable<SearchRun> searchQueue = strategy.GetSearchQueue();

            var runsByRadius = searchQueue
                .GroupBy(x => x.RadiusDegrees)
                .OrderByDescending(x => x.Key)
                .Select(g => g.ToList())
                .ToList();
            var radiusGroupCount = runsByRadius.Count;


            if (strategy.UseParallelism)
            {
                
                try
                {
                    Task<SolveResult[]> whenAllResult = null;
                    
                    for (currentSubSetIndex = 0; currentSubSetIndex < numSubSets; currentSubSetIndex++)
                    {
                        
                        bool continueSearching = true;

                        for (var rg = 0; rg < radiusGroupCount; rg++)
                        {
                            // Console.WriteLine($"Group {rg} - {runsByRadius[rg].Count}");
                            //searchQueue = runsByRadius[rg];

                            var currentRadiusAreasCount = runsByRadius[rg].Count;
                            //var batchItemCount = 1000;
                            var batchItemCount =
                                currentRadiusAreasCount < 3000 ? 1000 :
                                currentRadiusAreasCount < 25000 ? 2500 :
                                4000;
                            var areaBatches =
                                new List<SearchRun>[(int)Math.Ceiling((double)runsByRadius[rg].Count / batchItemCount)];
                            for (var areaBatch = 0; areaBatch < areaBatches.Length; areaBatch++)
                                areaBatches[areaBatch] = new List<SearchRun>(batchItemCount);
                            for (var areaIndex = 0; areaIndex < runsByRadius[rg].Count; areaIndex++)
                            {
                                areaBatches[areaIndex / batchItemCount].Add(runsByRadius[rg][areaIndex]);
                            }


                            for (var areaBatchIndex = 0; areaBatchIndex < areaBatches.Length; areaBatchIndex++)
                            {
                                if (combinedCts.Token.IsCancellationRequested)
                                {
                                    continueSearching = false;
                                    break;
                                }

                                searchQueue = areaBatches[areaBatchIndex];
                                
                                _logger.WriteInfo(
                                    $"Starting search tasks in parallel. Running sampling subset {currentSubSetIndex + 1}/{numSubSets}");

                                
                                var searchTasks = searchQueue.Select(searchRun => WatneyTaskFactory.Instance.StartNew(() =>
                                    TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass,
                                        quadDb,
                                        numSubSets, currentSubSetIndex, imageStarQuads,
                                        completionCts)));
                                
                                whenAllResult = Task.WhenAll(searchTasks);
                                await whenAllResult;

                                var resultSet = GetMatchedAndUnmatchedSearchRuns(whenAllResult.Result);

                                successfulSolveResult = resultSet.withMatches.FirstOrDefault(x => x != null && x.Success);
                                continueSearching = successfulSolveResult == null && !combinedCts.Token.IsCancellationRequested;
                                
                                if (!continueSearching)
                                    break;

                                // If only one subset aka no sampling, no need to check potential matches since they aren't potential as
                                // we're already using all database quads in our search.
                                if (numSubSets == 1)
                                    continue;


                                var potentialMatchQueue = resultSet.withMatches.Select(x => x.SearchRun).ToArray();

                                // Remove all potentials so that we don't search them again in the next subset.
                                // They aren't a significant number, but every little bit helps.
                                for (var m = 0; m < potentialMatchQueue.Length; m++) 
                                    runsByRadius[rg].Remove(potentialMatchQueue[m]);

                                // Console.WriteLine($"Continue searching, potential matches to try: {potentialMatchQueue.Length}");
                                _logger.WriteInfo(
                                    $"Continue searching, potential matches to try: {potentialMatchQueue.Length}");
                                var potentialMatchSearchTasks = potentialMatchQueue.Select(searchRun =>
                                    WatneyTaskFactory.Instance.StartNew(() =>
                                        TrySolveSingle(imageDimensions, combinedCts, searchRun,
                                            countInFirstPass,
                                            quadDb, 1, 0, imageStarQuads,
                                            completionCts)));

                                whenAllResult = Task.WhenAll(potentialMatchSearchTasks);
                                await whenAllResult;

                                successfulSolveResult = whenAllResult.Result.FirstOrDefault(x => x != null && x.Success);
                                continueSearching = successfulSolveResult == null && !combinedCts.Token.IsCancellationRequested;

                                if (successfulSolveResult != null)
                                    _logger.WriteInfo($"A successful result was found!");


                                if (!continueSearching)
                                    break;
                                
                                
                            }
                            
                            if (!continueSearching)
                                break;

                            _logger.WriteInfo($"Radius group {rg} in subset {currentSubSetIndex+1}/{numSubSets} yielded no result, continuing to next radius group.");
                        }

                        if (!continueSearching)
                            break;
                    }

                }
                finally
                {
                    stopwatch.Stop();
                    _logger.WriteInfo($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                    result = successfulSolveResult ?? new SolveResult();
                    diagnosticsData.MatchInstances = result.DiagnosticsData?.MatchInstances;
                    result.StarsDetected = stars.Count;
                    result.StarsUsedInSolve = chosenDetectedStars.Count;
                    result.DiagnosticsData = diagnosticsData;
                    result.TimeSpent = stopwatch.Elapsed;
                    result.AreasSearched = _iterations;
                    if (cancellationToken.IsCancellationRequested)
                        result.Canceled = true;
                }
                
                
            }
            else
            {
                _logger.WriteInfo($"Starting search tasks in serial mode");
                var serialSearches = new List<SolveResult>();

                // For convenience.
                void MakeSuccessResult(SolveResult r)
                {
                    result = r;
                    diagnosticsData.MatchInstances = r.DiagnosticsData.MatchInstances;
                    result.StarsDetected = stars.Count;
                    result.StarsUsedInSolve = chosenDetectedStars.Count;
                    result.DiagnosticsData = diagnosticsData;
                    result.TimeSpent = stopwatch.Elapsed;
                    result.AreasSearched = _iterations;
                }
                

                for (currentSubSetIndex = 0; currentSubSetIndex < numSubSets; currentSubSetIndex++)
                {
                    bool continueSearching = true;

                    for (var rg = 0; rg < radiusGroupCount; rg++)
                    {
                        // Console.WriteLine($"Group {rg} - {runsByRadius[rg].Count}");

                        var batchItemCount = 1000;
                        var areaBatches =
                            new List<SearchRun>[(int)Math.Ceiling((double)runsByRadius[rg].Count / batchItemCount)];
                        for (var areaBatch = 0; areaBatch < areaBatches.Length; areaBatch++)
                            areaBatches[areaBatch] = new List<SearchRun>(1000);
                        for (var areaIndex = 0; areaIndex < runsByRadius[rg].Count; areaIndex++)
                        {
                            areaBatches[areaIndex / batchItemCount].Add(runsByRadius[rg][areaIndex]);
                        }


                        for (var areaBatchIndex = 0; areaBatchIndex < areaBatches.Length; areaBatchIndex++)
                        {
                            searchQueue = areaBatches[areaBatchIndex];
                            SolveResult taskResult = null;

                            foreach (var searchRun in searchQueue)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    continueSearching = false;
                                    break;
                                }

                                taskResult = TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass,
                                    quadDb, numSubSets, currentSubSetIndex,
                                    imageStarQuads,
                                    completionCts);

                                serialSearches.Add(taskResult);

                                if (taskResult != null && taskResult.Success)
                                {
                                    stopwatch.Stop();
                                    _logger.WriteInfo($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                                    MakeSuccessResult(taskResult);

                                    continueSearching = false;
                                    break;
                                }
                            }

                           
                            if (!continueSearching)
                                break;

                            // If only one subset aka no sampling, no need to check potential matches since they aren't potential as
                            // we're already using all database quads in our search.
                            if (numSubSets == 1)
                                continue;

                            var resultSet = GetMatchedAndUnmatchedSearchRuns(serialSearches.ToArray());
                            serialSearches.Clear();

                            var potentialMatchQueue = resultSet.withMatches.Select(x => x.SearchRun).ToArray();
                            _logger.WriteInfo($"Continue searching, potential matches to try: {potentialMatchQueue.Length}");

                            // Remove all potentials so that we don't search them again in the next subset.
                            // They aren't a significant number, but every little bit helps.
                            for (var m = 0; m < potentialMatchQueue.Length; m++) 
                                runsByRadius[rg].Remove(potentialMatchQueue[m]);
                            
                            foreach (var searchRun in potentialMatchQueue)
                            {
                                taskResult = TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass,
                                    quadDb, 1, 0, imageStarQuads,
                                    completionCts);

                                if (taskResult != null && taskResult.Success)
                                {
                                    stopwatch.Stop();
                                    _logger.WriteInfo($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                                    MakeSuccessResult(taskResult);
                                    
                                    break;
                                }
                            }

                            successfulSolveResult = result;
                            continueSearching = successfulSolveResult == null && !combinedCts.Token.IsCancellationRequested;

                            if (successfulSolveResult != null)
                            {
                                _logger.WriteInfo($"A successful result was found!");
                                break;
                            }

                            if (!continueSearching)
                                break;
                            

                        }

                        if (!continueSearching)
                            break;

                        _logger.WriteInfo($"Radius group {rg} in subset {currentSubSetIndex+1}/{numSubSets} yielded no result, continuing to next radius group.");
                    }

                    if (!continueSearching)
                        break;

                    _logger.WriteInfo($"Subset {currentSubSetIndex+1}/{numSubSets} yielded no result, continuing to next subset.");
                }

                if (result == null)
                {
                    stopwatch.Stop();
                    _logger.WriteInfo($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                    result = new SolveResult();
                    result.StarsDetected = stars.Count;
                    result.StarsUsedInSolve = chosenDetectedStars.Count;
                    result.DiagnosticsData = diagnosticsData;
                    result.AreasSearched = _iterations;
                    result.TimeSpent = stopwatch.Elapsed;
                    if (cancellationToken.IsCancellationRequested)
                        result.Canceled = true;
                }

                
            }

            OnSolveProgress?.Invoke(SolverStep.SolveProcessFinished);

            quadDb.DisposeSolveContext(_solveContextId);

            return result;
            

        }

        private (SolveResult[] withMatches, SolveResult[] withoutMatches) GetMatchedAndUnmatchedSearchRuns(
            SolveResult[] results)
        {
            var withMatches = new List<SolveResult>(results.Length);
            var withoutMatches = new List<SolveResult>(results.Length);
            for (var i = 0; i < results.Length; i++)
            {
                if(results[i] == null)
                    continue;
                
                if(results[i].NumPotentialMatches > 0)
                    withMatches.Add(results[i]);
                else
                    withoutMatches.Add(results[i]);
            }
            
            return (withMatches.OrderByDescending(x => x.NumPotentialMatches).ToArray(), withoutMatches.ToArray());
        }

        private SolveResult TrySolveSingle(IImageDimensions imageDimensions, CancellationTokenSource cancellationCts, SearchRun searchRun,
            int countInFirstPass, IQuadDatabase quadDb, int numSubSets, int subSetIndex, ImageStarQuad[] imageStarQuads, CancellationTokenSource completionCts)
        {
            if (cancellationCts.IsCancellationRequested)
                return null;

            SolveResult taskResult = new SolveResult()
            {
                Success = false,
                SearchRun = searchRun,
                DiagnosticsData = new SolveDiagnosticsData() // this will mostly be filled outside this method.
            };

            
            Interlocked.Increment(ref _iterations);
            var iteration = _iterations;
            var logPrefix = $"Iteration {iteration} {searchRun}:";
            
            // Quads per degree
            var searchFieldSize = searchRun.RadiusDegrees * 2;
            var a = Math.Atan((double) imageDimensions.ImageHeight / imageDimensions.ImageWidth);
            var s1 = searchFieldSize * Math.Sin(a);
            var s2 = searchFieldSize * Math.Cos(a);
            var area = s1 * s2;
            var quadsPerSqDeg = countInFirstPass / area; 

            double imageDiameterInPixels = Math.Sqrt(imageDimensions.ImageHeight * imageDimensions.ImageHeight +
                                                     imageDimensions.ImageWidth * imageDimensions.ImageWidth);
            var pixelAngularSearchFieldSizeRatio = imageDiameterInPixels / searchFieldSize;


            //int minMatches = 5;
            int minMatches = 5;

            List<StarQuadMatch> matchingQuads = null;
            
            var databaseQuads =
                    quadDb.GetQuads(searchRun.Center, searchRun.RadiusDegrees, (int)quadsPerSqDeg,
                        searchRun.DensityOffsets, numSubSets, subSetIndex, imageStarQuads, _solveContextId);

            taskResult.NumPotentialMatches = databaseQuads.Count;

            _logger.WriteInfo($"{logPrefix} {databaseQuads.Count} potential database matches");
            if (databaseQuads.Count < minMatches)
            {
                return taskResult;
            }

            // Found enough matches; a likely hit. If this was a sampled run, spend the time to retrieve the full quad set without sampling
            // as we're going to try for a solution.
            if (numSubSets > 1)
            {
                databaseQuads = quadDb.GetQuads(searchRun.Center, searchRun.RadiusDegrees,
                    (int)quadsPerSqDeg, searchRun.DensityOffsets, 1, 0,
                    imageStarQuads, _solveContextId);
            }

            matchingQuads = FindMatches(pixelAngularSearchFieldSizeRatio, imageStarQuads, databaseQuads, 0.011, minMatches);

            if (matchingQuads.Count >= minMatches)
            {
                _logger.WriteInfo($"{logPrefix} {matchingQuads.Count} image-catalog matches, attempting to calculate solution");
                var preliminarySolution = CalculateSolution(imageDimensions, matchingQuads, searchRun.Center, out _);

                if (!IsValidSolution(preliminarySolution))
                {
                    _logger.WriteInfo($"{logPrefix} not a valid solution");
                    return taskResult;
                }

                Interlocked.Increment(ref _tentativeMatches);

                // Probably off really badly, so don't accept it.
                if (preliminarySolution.Radius > 2 * searchRun.RadiusDegrees)
                {
                    return taskResult;
                }

                pixelAngularSearchFieldSizeRatio = imageDiameterInPixels / preliminarySolution.Radius * 2;


                _logger.WriteInfo($"{logPrefix} valid solution, calculating an improved solution");
                // Calculate a second time; we may be quite a bit off if we're detecting the quads at an edge,
                // so calculating it a second time with the center and radius of the first solution
                // should improve our accuracy.
                var improvedSolution = PerformAccuracyImprovementForSolution(imageDimensions, preliminarySolution,
                    pixelAngularSearchFieldSizeRatio, quadDb, imageStarQuads, (int) quadsPerSqDeg, minMatches, searchRun.DensityOffsets);

                if (!IsValidSolution(improvedSolution.solution))
                {
                    _logger.WriteInfo($"{logPrefix} solution improve failed");
                    return taskResult;
                }

                _logger.WriteInfo($"{logPrefix} valid solution was found");
                taskResult.Success = true;
                taskResult.Canceled = false;
                taskResult.SearchRun = searchRun;
                taskResult.MatchedQuads = improvedSolution.matches.Count;
                taskResult.Solution = improvedSolution.solution;
                taskResult.DiagnosticsData.MatchInstances = improvedSolution.matches;
                completionCts.Cancel();
                return taskResult;

            }

            return taskResult;

        }

        private bool IsValidSolution(Solution s)
        {
            if (s == null)
                return false;

            var pcs = new[]
            {
                s.PlateConstants.A, s.PlateConstants.B, s.PlateConstants.C, s.PlateConstants.D, s.PlateConstants.E,
                s.PlateConstants.F
            };
            if (double.IsNaN(s.PlateCenter.Ra) || double.IsNaN(s.PlateCenter.Dec) ||
                double.IsNaN(s.Orientation) || pcs.Any(x => double.IsNaN(x) || double.IsInfinity(x)))
                return false;
            return true;
        }

        private (Solution solution, List<StarQuadMatch> matches) PerformAccuracyImprovementForSolution(IImageDimensions imageDimensions, Solution solution,
            double pixelAngularSearchFieldSizeRatio,
            IQuadDatabase quadDatabase, ImageStarQuad[] imageStarQuads, int quadsPerSqDeg, int minMatches, int[] densityOffsets)
        {
            var resolvedCenter = solution.PlateCenter;
            // var fullDensityOffsets = new[] {-5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5}; // Include many, to improve the odds and to maximize match chances.
            // Actually, use given offsets. The result is likely going to be more accurate on large fields due to smaller chance of mismatches.
            var databaseQuads = quadDatabase.GetQuads(resolvedCenter, solution.Radius, quadsPerSqDeg, densityOffsets, 1, 0, imageStarQuads, _solveContextId);
            var matchingQuads = FindMatches(pixelAngularSearchFieldSizeRatio, imageStarQuads, databaseQuads, 0.011, minMatches);
            if (matchingQuads.Count >= minMatches)
                return (CalculateSolution(imageDimensions, matchingQuads, resolvedCenter, out var acceptedMatches), acceptedMatches);
            return (null, null);
        }

        /// <summary>
        /// This method filters out matches that are "suspicious" or "bad" (for the most part), and only keeps
        /// the ones that are representing best quality. This results in a smaller number of used matches but this should
        /// still result in a better overall result.
        /// 
        /// This is mostly to mitigate the problem of large fields with massive amounts of stars, and resolution
        /// simply being not up to par with the needs of accurate calculations. The more stars, the more matches,
        /// the bigger the field and the smaller the resolution, the more effect a single pixel has. There's also
        /// a high chance for mismatches (a match in the area is found thanks to inaccuracies, and the (x,y) -> (ra,dec)
        /// coordinates don't correlate with the majority of results).
        /// Without this method, large fields e.g. 5deg with 14k+ detected stars may not really solve correctly, as
        /// erroneous matches within the field start to manifest and start throwing off the solution. The more stars
        /// in the field and the more stars included in the solve, the more issues we start seeing.
        ///
        /// What we do here is we inspect each individual match's effect on the overall solution. If a match closely
        /// correlates with the rest of the matches, it should affect the overall solution very little. If a match does
        /// not correlate well, it affects the solution significantly more and throws it off. The more we have those, the more
        /// it throws off the overall solution.
        ///
        /// We check each Plate Constant, as well as the Scale Ratio (image quad scale (px) compared to db quad scale (deg)).
        /// Right now we allow 2 * sigma variance for the plate constants. We have the preliminary solution using all the
        /// matches, and then we remove matches one by one and see the effect they are having on the solution. If it
        /// seems to go over the variance limits, we drop it.
        /// Depending on the effective resolution of the image (scale ratio) we limit the accepted scale ratio deviation
        /// from the median scale ratio. For small scale ratios we have a stricter limit, and for larger scale ratios
        /// we have a more relaxed limit.
        /// </summary>
        /// <param name="matches"></param>
        /// <param name="initialPlateConstants"></param>
        /// <param name="scopeCoords"></param>
        /// <returns></returns>
        private List<StarQuadMatch> ReduceToBestMatches(List<StarQuadMatch> matches, PlateConstants initialPlateConstants, EquatorialCoords scopeCoords)
        {
            var filtered = new List<StarQuadMatch>();
            _logger.WriteInfo($"Reducing matches for improved accuracy, processing {matches.Count} matches");


            double squaredSumsA = 0;
            double squaredSumsB = 0;
            double squaredSumsC = 0;
            double squaredSumsD = 0;
            double squaredSumsE = 0;
            double squaredSumsF = 0;
            double squaredSumsSR = 0;

            var matchCount = matches.Count;

            var scaleRatios = matches.Select(x => x.ScaleRatio).ToArray();
            Array.Sort(scaleRatios);
            int midIndex = scaleRatios.Length / 2;
            var medianScaleRatio = (scaleRatios.Length % 2 != 0)
                ? scaleRatios[midIndex]
                : (scaleRatios[midIndex] + scaleRatios[midIndex - 1]) / 2;

            var matchDeltas = new PlateConstants[matchCount];
            var matchMedianScaleRatioDeviances = new double[matchCount];

            // Calculate the effect of each match on the solution.
            // Remove those that cause effects above the thresholds as they may be mismatches within the area.
            // This can mainly happen with large fields with absolutely massive amounts of stars visible.
            // Same applies to scale ratios; we expect to have nearly constant scale ratios on our matches. Highly
            // varying scale ratios imply something is wrong.

            for(var i = 0; i < matchCount; i++)
            {
                var match = matches[i];
                var matchListCopy = new List<StarQuadMatch>(matches);
                matchListCopy.Remove(match);
                var pc = SolvePlateConstants(matchListCopy, scopeCoords);

                var deltas = new PlateConstants
                {
                    A = pc.A - initialPlateConstants.A,
                    B = pc.B - initialPlateConstants.B,
                    C = pc.C - initialPlateConstants.C,
                    D = pc.D - initialPlateConstants.D,
                    E = pc.E - initialPlateConstants.E,
                    F = pc.F - initialPlateConstants.F
                };
                matchDeltas[i] = deltas;
                matchMedianScaleRatioDeviances[i] = Math.Abs(medianScaleRatio - match.ScaleRatio);

                squaredSumsA += deltas.A * deltas.A;
                squaredSumsB += deltas.B * deltas.B;
                squaredSumsC += deltas.C * deltas.C;
                squaredSumsD += deltas.D * deltas.D;
                squaredSumsE += deltas.E * deltas.E;
                squaredSumsF += deltas.F * deltas.F;
                squaredSumsSR += (medianScaleRatio - match.ScaleRatio) * (medianScaleRatio - match.ScaleRatio);


            }

            var deviationThresholdA = 2 * Math.Sqrt(squaredSumsA / matchCount);
            var deviationThresholdB = 2 * Math.Sqrt(squaredSumsB / matchCount);
            var deviationThresholdC = 2 * Math.Sqrt(squaredSumsC / matchCount);
            var deviationThresholdD = 2 * Math.Sqrt(squaredSumsD / matchCount);
            var deviationThresholdE = 2 * Math.Sqrt(squaredSumsE / matchCount);
            var deviationThresholdF = 2 * Math.Sqrt(squaredSumsF / matchCount);

            var deviationThresholdSRSigma = Math.Sqrt(squaredSumsSR / matchCount);
            
            // The higher the ScaleRatio, the more leeway we can give (better effective resolution)
            //
            // Experimental values.
            // Minimum tolerated scale ratio deviation (sigma)
            var minSrSigma = 0.66;
            // Maximum tolerated scale ratio deviation (sigma)
            var maxSrSigma = 2.0;
            // Scale ratio of large fields/low resolution, border value
            var minSrAtValue = 300.0;
            // Scale ratio of smaller fields/high resolution, border value
            var maxSrAtValue = 1500.0;
            
            // Linear interpolation
            double Lerp(double v0, double v1, double t) => (1 - t) * v0 + t * v1;

            var deviationThresholdScaleRatioSigmaFactor = 
                medianScaleRatio <= minSrAtValue 
                    ? minSrSigma
                    : medianScaleRatio >= maxSrAtValue 
                        ? maxSrSigma
                        : Lerp(minSrSigma, maxSrSigma, (medianScaleRatio - minSrAtValue) / (maxSrAtValue - minSrAtValue));

            var deviationThresholdScaleRatio = deviationThresholdSRSigma * deviationThresholdScaleRatioSigmaFactor;
            

            _logger.WriteInfo($"Scale ratio median: {medianScaleRatio}");
            _logger.WriteInfo($"Scale ratio deviance threshold: {deviationThresholdScaleRatio}");
            _logger.WriteInfo($"Scale ratio deviance threshold sigma factor: {deviationThresholdScaleRatioSigmaFactor}");

            for (var i = 0; i < matchDeltas.Length; i++)
            {
                var match = matches[i];
                var scaleRatioDeviance = matchMedianScaleRatioDeviances[i];
                var deltas = matchDeltas[i];

                if (Math.Abs(deltas.A) > deviationThresholdA || Math.Abs(deltas.B) > deviationThresholdB ||
                    Math.Abs(deltas.C) > deviationThresholdC || Math.Abs(deltas.D) > deviationThresholdD ||
                    Math.Abs(deltas.E) > deviationThresholdE || Math.Abs(deltas.F) > deviationThresholdF ||
                    scaleRatioDeviance > deviationThresholdScaleRatio)
                {
                    continue;
                }
                    
                filtered.Add(match);
            }

            if (filtered.Count >= 8)
            {
                _logger.WriteInfo($"Filtered out {matches.Count - filtered.Count} matches that could have caused undesired effect on accuracy");
                return filtered;
            }
            else
            {
                _logger.WriteInfo($"Not enough matches to perform filtering, with so few matches assuming they're good");
                return matches;
            }
            

        }

        private Solution CalculateSolution(IImageDimensions imageDimensions, List<StarQuadMatch> matches, EquatorialCoords scopeCoords, out List<StarQuadMatch> acceptedMatches)
        {
            var imgW = imageDimensions.ImageWidth;
            var imgH = imageDimensions.ImageHeight;
            var pc = SolvePlateConstants(matches, scopeCoords);
            
            // Filter out the matches further - there may still be mismatches that are distorting the solution.
            acceptedMatches = ReduceToBestMatches(matches, pc, scopeCoords);
            pc = SolvePlateConstants(acceptedMatches, scopeCoords);
            matches = acceptedMatches;
            
            var pixelsPerDeg = CalculatePixelsPerDegree(matches);
            var fieldPixelDiameter = Math.Sqrt(imgW * imgW + imgH * imgH);
            var fieldPixelRadius = 0.5 * fieldPixelDiameter;
            var fieldRadiusDeg = 0.5 * fieldPixelDiameter / pixelsPerDeg;
            var fieldWidthDeg = imageDimensions.ImageWidth / pixelsPerDeg;
            var fieldHeightDeg = imageDimensions.ImageHeight / pixelsPerDeg;

            // Arcseconds per pixel
            var pixScale = 3600 * fieldRadiusDeg / fieldPixelRadius;

            // Calculate the transformation matrix by calculating what one pixel move accounts
            // for in both x and y directions.
            var centerX = imgW / 2;
            var centerY = imgH / 2;
            var X = pc.A * centerX + pc.B * centerY + pc.C;
            var Y = pc.D * centerX + pc.E * centerY + pc.F;
            var imageCenterEquatorial = EquatorialCoords.StandardToEquatorial(scopeCoords, X, Y);
            
            // One pixel step in the positive Y direction
            X = pc.A * centerX + pc.B * (centerY + 1) + pc.C;
            Y = pc.D * centerX + pc.E * (centerY + 1) + pc.F;
            var upEquatorial = EquatorialCoords.StandardToEquatorial(scopeCoords, X, Y);

            // One pixel step in the positive X direction
            X = pc.A * (centerX + 1) + pc.B * centerY + pc.C;
            Y = pc.D * (centerX + 1) + pc.E * centerY + pc.F;
            var rightEquatorial = EquatorialCoords.StandardToEquatorial(scopeCoords, X, Y);

            // Calculate the matrix components CD* that can be used to calculate any pixel RA/Dec
            // TODO: find the source where the heck did I originally pick these up?
            var dRa = Conversions.Deg2Rad(rightEquatorial.Ra - imageCenterEquatorial.Ra);
            var cd1_1 = Conversions.Rad2Deg(dRa * Math.Cos(Conversions.Deg2Rad(imageCenterEquatorial.Dec)));
            var cd2_1 = rightEquatorial.Dec - imageCenterEquatorial.Dec;

            dRa = Conversions.Deg2Rad(upEquatorial.Ra - imageCenterEquatorial.Ra);
            var cd1_2 = Conversions.Rad2Deg(dRa * Math.Cos(Conversions.Deg2Rad(imageCenterEquatorial.Dec)));
            var cd2_2 = upEquatorial.Dec - imageCenterEquatorial.Dec;


            var crota1 = 0.0;
            var crota2 = 0.0;
            var cdelt1 = cd1_1;
            var cdelt2 = cd2_2;

            // https://www.virtualastronomy.org/AVM_DRAFTVersion12_rlh02.pdf

            // The determinant, which also tells us if the image is mirrored.
            var sign = cd1_1 * cd2_2 - cd1_2 * cd2_1 < 0 ? -1 : 1;
            if (cd2_1 != 0 && cd1_2 != 0)
            {
                crota1 = Conversions.Rad2Deg(Math.Atan2(sign * cd1_2, cd2_2));
                crota2 = -Conversions.Rad2Deg(Math.Atan2(cd2_1, sign * cd1_1));
                cdelt1 = sign * Math.Sqrt(cd1_1 * cd1_1 + cd2_1 * cd2_1);
                cdelt2 = Math.Sqrt(cd1_2 * cd1_2 + cd2_2 * cd2_2);
            }

            var scopePxY = (-(-pc.A / pc.D * pc.F) + -pc.C) /
                    (pc.B + -pc.A / pc.D * pc.E);
            var scopePxX = (-pc.B * scopePxY + (-pc.C)) / pc.A;
            
            var parity = sign < 0 ? Parity.Normal : Parity.Flipped; 

            var solution = new Solution(scopeCoords, imageCenterEquatorial, imgW, imgH, pixScale,
                fieldWidthDeg, fieldHeightDeg, fieldRadiusDeg, pc, cdelt1, cdelt2, crota1, crota2, cd1_1, cd2_1, cd1_2,
                cd2_2, scopeCoords.Ra, scopeCoords.Dec, scopePxX, scopePxY, parity);

            
            return solution;

        }

        // todo is this used anymore?
        EquatorialCoords GetPixelEquatorialCoords(double cd1_1, double cd2_1, double cd1_2, double cd2_2, (double x, double y) refPix, 
            EquatorialCoords refCoords, (double x, double y) pixelPos)
        {

            var pxDx = pixelPos.x - refPix.x;
            var pxDy = pixelPos.y - refPix.y;
            var dRaRad = Conversions.Deg2Rad(cd1_1 * pxDx + cd1_2 * pxDy);
            var dDecRad = Conversions.Deg2Rad(cd2_1 * pxDx + cd2_2 * pxDy);
            var refDecRad = Conversions.Deg2Rad(refCoords.Dec);
            
            var d = Math.Cos(refDecRad) - dDecRad * Math.Sin(refDecRad);
            var g = Math.Sqrt(dRaRad * dRaRad + d * d);

            var pixelRa = refCoords.Ra + Conversions.Rad2Deg(Math.Atan2(dRaRad, d));
            var pixelDec = Conversions.Rad2Deg(Math.Atan((Math.Sin(refDecRad) + dDecRad * Math.Cos(refDecRad)) / g));

            return new EquatorialCoords(pixelRa, pixelDec);
        }
        

        private double CalculatePixelsPerDegree(List<StarQuadMatch> matches)
        {
            List<double> pixelsPerDegree = new List<double>();
            for (var i = 0; i < matches.Count; i++)
            {
                for (var j = i+1; j < matches.Count; j++)
                {
                    var dx = matches[i].ImageStarQuad.PixelMidPoint.x - matches[j].ImageStarQuad.PixelMidPoint.x;
                    var dy = matches[i].ImageStarQuad.PixelMidPoint.y - matches[j].ImageStarQuad.PixelMidPoint.y;
                    var pixelDist = Math.Sqrt(dx * dx + dy * dy);

                    var angularDist = matches[i].CatalogStarQuad.MidPoint
                        .GetAngularDistanceTo(matches[j].CatalogStarQuad.MidPoint);


                    if(angularDist > 0 && pixelDist > 0) 
                        pixelsPerDegree.Add(pixelDist / angularDist);
                }
            }

            return pixelsPerDegree.Average();
        }

        private PlateConstants SolvePlateConstants(List<StarQuadMatch> matches, EquatorialCoords center)
        {
            // StdCoordX = ax + by + c  -->  ax + by + c - StdCoordX = 0
            // StdCoordY = dx + ey + f  -->  dx + ey + f - StdCoordY = 0;

            var xEquationInputs = matches.Select(match =>
            {
                var x = match.ImageStarQuad.PixelMidPoint.x;
                var y = match.ImageStarQuad.PixelMidPoint.y;
                var z = 1.0;
                var constant = match.CatalogStarQuad.MidPoint.ToStandardCoordinates(center).x;
                return (x, y, z, constant);
            }).ToArray();

            var yEquationInputs = matches.Select(match =>
            {
                var x = match.ImageStarQuad.PixelMidPoint.x; 
                var y = match.ImageStarQuad.PixelMidPoint.y;
                var z = 1.0;
                var constant = match.CatalogStarQuad.MidPoint.ToStandardCoordinates(center).y;
                return (x, y, z, constant);
            }).ToArray();

            (double a, double b, double c) xConstants = Equations.SolveLeastSquares(xEquationInputs);
            (double d, double e, double f) yConstants = Equations.SolveLeastSquares(yEquationInputs);

            return new PlateConstants
            {
                A = xConstants.a,
                B = xConstants.b,
                C = xConstants.c,
                D = yConstants.d,
                E = yConstants.e,
                F = yConstants.f
            };

            
        }
        
        private List<StarQuadMatch> FindMatches(double pixelToAngleRatio, IList<ImageStarQuad> imageQuads, List<StarQuad> dbQuads, double threshold, int minMatches)
        {
            var matches = new ConcurrentBag<StarQuadMatch>();
            
            var batchSize = 5;
            var imageQuadBatches = new List<ImageStarQuad[]>();
            var batch = new ImageStarQuad[batchSize];
            for (var i = 0; i < imageQuads.Count; i++)
            {
                batch[i % batchSize] = imageQuads[i];
                if ((i+1) % batchSize == 0 || i == imageQuads.Count - 1)
                {
                    imageQuadBatches.Add(batch);
                    batch = new ImageStarQuad[batchSize];
                }
            }

            for (var b = 0; b < imageQuadBatches.Count; b++)
            {
                var dbQuadsFound = new ConcurrentBag<StarQuad>();

                Parallel.For(0, 5, i =>
                {
                    var imageQuad = imageQuadBatches[b][i];
                    if (imageQuad == null) return;
                    for (var j = 0; j < dbQuads.Count; j++)
                    {
                        var d1 = Math.Abs(imageQuad.Ratios[0] / dbQuads[j].Ratios[0] - 1.0);
                        if (d1 > threshold) continue;
                        var d2 = Math.Abs(imageQuad.Ratios[1] / dbQuads[j].Ratios[1] - 1.0);
                        if (d2 > threshold) continue;
                        var d3 = Math.Abs(imageQuad.Ratios[2] / dbQuads[j].Ratios[2] - 1.0);
                        if (d3 > threshold) continue;
                        var d4 = Math.Abs(imageQuad.Ratios[3] / dbQuads[j].Ratios[3] - 1.0);
                        if (d4 > threshold) continue;
                        var d5 = Math.Abs(imageQuad.Ratios[4] / dbQuads[j].Ratios[4] - 1.0);
                        if (d5 > threshold) continue;
                        
                        matches.Add(new StarQuadMatch(dbQuads[j], imageQuad));
                        dbQuadsFound.Add(dbQuads[j]);
                        break; // This is a must; we have observed remote possibility of duplicates. And one pixel coordinate should match exactly one quad.
                    }

                });

                foreach (var q in dbQuadsFound)
                    dbQuads.Remove(q);
            }
            
            
            if (matches.Count < minMatches)
                return new List<StarQuadMatch>();

            // Ratios' median absolute deviance shouldn't be off more than this, if it is, then we're probably having a wild set of mismatches.
            var acceptedAbsoluteDev = pixelToAngleRatio * 0.01;

            var matchList = matches.ToArray();
            Array.Sort(matchList, new StarQuadMatch.StarQuadMatchScaleRatioSorter());
            int midIndex = matchList.Length / 2;
            var medianScaleRatio = (matchList.Length % 2 != 0) 
                ? matchList[midIndex].ScaleRatio 
                : (matchList[midIndex].ScaleRatio + matchList[midIndex - 1].ScaleRatio) / 2;

            var scaleRatioAbsoluteDeviances = new double[matchList.Length];
            for (var i = 0; i < scaleRatioAbsoluteDeviances.Length; i++)
            {
                scaleRatioAbsoluteDeviances[i] = Math.Abs(matchList[i].ScaleRatio - medianScaleRatio);
            }
            Array.Sort(scaleRatioAbsoluteDeviances);
            var medianAbsoluteDevianceScaleRatio = (scaleRatioAbsoluteDeviances.Length % 2 != 0)
                ? scaleRatioAbsoluteDeviances[midIndex]
                : (scaleRatioAbsoluteDeviances[midIndex] + scaleRatioAbsoluteDeviances[midIndex - 1]) / 2;

            // If the scale ratios are wildly random, this can't be a match.
            if(medianAbsoluteDevianceScaleRatio > acceptedAbsoluteDev)
                return new List<StarQuadMatch>();


            // Form bins of the matches, and calculate a weighted average using the bins.
            // This is to make sure the mismatches (wild scale ratios) do not affect the
            // average in an unreasonable manner.

            var minScaleRatio = matchList[0].ScaleRatio;
            var maxScaleRatio = matchList[matchList.Length - 1].ScaleRatio;
            var numBins = 10;
            var binWidth = (maxScaleRatio - minScaleRatio) / numBins + 1;
            var weights = new int[numBins];
            var indexWeights = new int[matchList.Length];

            for (var i = 0; i < matchList.Length; i++)
            {
                var w = (int) ((matchList[i].ScaleRatio - minScaleRatio) / binWidth);
                weights[w]++;
                indexWeights[i] = w;
            }

            var weightedMean = 0.0;
            var divider = 0.0;
            var total = 0.0;

            for (var i = 0; i < matchList.Length; i++)
            {
                var weight = weights[indexWeights[i]];
                total += weight * matchList[i].ScaleRatio;
                divider += weight;
            }

            weightedMean = total / divider;
            var differenceSquared = matches.Select(m => (m.ScaleRatio - weightedMean) * (m.ScaleRatio - weightedMean));
            var stdDev = Math.Sqrt(differenceSquared.Average());

            
            return matches
                .Where(m => Math.Abs(m.ScaleRatio - weightedMean) < stdDev).ToList();
            
        }
        

        internal static (ImageStarQuad[] quads, int countInFirstPass) FormImageStarQuads(IList<ImageStar> starsFound)
        {
            var quads = new List<ImageStarQuad>();
            int countInFirstPass = 0;

            // Do a few passes. Experimental.
            var passes = 4;
            for (var p = 0; p < passes; p++)
            {
                //var starsToUse = (int) (starsFound.Count * (1 - p * 0.05));
                var starsToUse = (int) (starsFound.Count * (1 - p * 0.05));
                var stars = starsFound.Take(starsToUse).ToList();

                // Avoided using Linq here, as we run this method quite a few times.
                // Literally halved the time spent here by removing Linq and Dictionary usage.

                var starDistances = new double[stars.Count][];
                for (var i = 0; i < starDistances.Length; i++)
                    starDistances[i] = new double[starDistances.Length];

                for (var i = 0; i < stars.Count; i++)
                {
                    starDistances[i][i] = 0;
                    for (var j = i + 1; j < stars.Count; j++)
                    {
                        var dist = stars[i].CalculateDistance(stars[j]);
                        starDistances[i][j] = dist;
                        starDistances[j][i] = dist;
                    }
                }

                for (var i = 0; i < starDistances.Length; i++)
                {
                    var starIndex0 = i;
                    var distancesToOthers = starDistances[starIndex0];

                    // Get 3 nearest
                    var nearestIndices = new int[3] { -1, -1, -1 };
                    var nearestDistances = new double[3];

                    for (var n = 0; n < 3; n++)
                    {
                        int index = 0;
                        var dist = double.MaxValue;
                        for (var j = 0; j < distancesToOthers.Length; j++)
                        {
                            if (distancesToOthers[j] < dist && distancesToOthers[j] > 0 && j != nearestIndices[0] && j != nearestIndices[1])
                            {
                                dist = distancesToOthers[j];
                                index = j;
                            }
                        }

                        nearestIndices[n] = index;
                        nearestDistances[n] = dist;
                    }


                    var d0a = nearestDistances[0];
                    var d0b = nearestDistances[1];
                    var d0c = nearestDistances[2];

                    var starIndexA = nearestIndices[0];
                    var starIndexB = nearestIndices[1];
                    var starIndexC = nearestIndices[2];

                    var dab = starDistances[starIndexA][starIndexB];
                    var dac = starDistances[starIndexA][starIndexC];
                    var dbc = starDistances[starIndexB][starIndexC];

                    var sixDistances = new List<double> { d0a, d0b, d0c, dab, dac, dbc };
                    sixDistances.Sort();
                    var largestDistance = sixDistances.Max();

                    sixDistances.RemoveAt(sixDistances.IndexOf(largestDistance));
                    var ratios = sixDistances
                        .Select(x => (float)(x / largestDistance))
                        .ToArray();
                    
                    var quadStars = new ImageStar[]
                    {
                        stars[starIndex0],
                        stars[starIndexA],
                        stars[starIndexB],
                        stars[starIndexC]
                    };

                    var quad = new ImageStarQuad(ratios, (float)largestDistance, quadStars);
                    quads.Add(quad);
                }

                if(p == 0)
                    countInFirstPass = quads.Distinct(new StarQuad.StarQuadStarBasedEqualityComparer()).ToArray().Length;
            }

            // var quadsArray = quads.Distinct(new StarQuad.StarQuadStarBasedEqualityComparer()).Cast<ImageStarQuad>().ToArray();
            var quadsArray = quads.Distinct(new ImageStarQuad.ImageStarQuadStarBasedEqualityComparer()).ToArray();

            //countInFirstPass = quadsArray.Length;
            return (quadsArray, countInFirstPass);
        }
    }
}