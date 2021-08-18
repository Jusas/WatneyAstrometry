﻿// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WatneyAstrometry.Core.Fits;
using WatneyAstrometry.Core.Image;
using WatneyAstrometry.Core.MathUtils;
using WatneyAstrometry.Core.QuadDb;
using WatneyAstrometry.Core.StarDetection;
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

        private IVerboseLogger _logger;
        
        public Solver(IVerboseLogger logger = null)
        {
            _logger = logger ?? new NullVerboseLogger();
            UseImageReader<DefaultFitsReader>(() => new DefaultFitsReader(), "fit", "fits");
        }

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

        public Solver RemoveImageReader<T>() where T : IImageReader
        {
            var type = typeof(T);
            var itemsToRemove = _imageReaderFactories.Where(kvp => kvp.Value.type == type)
                .Select(x => x.Key)
                .ToList();
            itemsToRemove.ForEach(x => _imageReaderFactories.Remove(x));
            return this;
        }

        public Solver ClearImageReaders()
        {
            _imageReaderFactories.Clear();
            return this;
        }

        public Solver UseStarDetector(Func<IStarDetector> factoryFunc)
        {
            _starDetectorFactory = factoryFunc;
            return this;
        }

        public Solver UseStarDetector(Func<Task<IStarDetector>> asyncFactoryFunc)
        {
            _starDetectorFactoryAsync = asyncFactoryFunc;
            return this;
        }

        public Solver UseQuadDatabase(Func<IQuadDatabase> factoryFunc)
        {
            _quadDatabaseFactory = factoryFunc;
            return this;
        }

        public Solver UseQuadDatabase(Func<Task<IQuadDatabase>> asyncFactoryFunc)
        {
            _quadDatabaseFactoryAsync = asyncFactoryFunc;
            return this;
        }

        /// <inheritdoc />
        public async Task<SolveResult> SolveFieldAsync(string filename, ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken)
        {
            _logger.Write($"Solving field from file {filename}, with strategy {strategy.GetType().Name}");
            var filenameExtension = Path.GetExtension(filename);
            if(string.IsNullOrEmpty(filenameExtension))
                throw new Exception("File does not have extension, unable to determine file type");

            filenameExtension = filenameExtension.Replace(".", string.Empty);
            if(!_imageReaderFactories.ContainsKey(filenameExtension))
                throw new Exception($"No ImageReader for file extension '{filenameExtension}' was found, unable to process image");

            var reader = _imageReaderFactories[filenameExtension].factory.Invoke();
            var image = reader.FromFile(filename);
            
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

            var detectedStars = starDetector.DetectStars(image);
            _logger.Write($"Detected {detectedStars.Count} from the image");

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
                : star.Brightness + (star.StarSize / medianSize) * 10)
                .Take(numStars)
                .ToList();
        }

        private static int ApproximateGoodSamplingValue(int chosenStarCount)
        {
            // For now, after some observations, a somewhat simple strategy.
            // When we have less stars, it's obvious that we can't deduct too much DB quads.
            // It looks like it's quite safe to go with a sampling of 3 in nearly all cases (assuming we have a good database).
            // With sampling 3 we mostly still get immediate matches, going up from that we'll have to fall back to check partial matches.
            // The less quads we need to compare to get partial matches, the more efficient the partial search is and the more time we shave off.
            // With more stars we use larger sampling values.

            // If stars < 100
            // use sampling of 3
            // if stars < 300
            // use sampling of 6
            // if stars >= 300 && < 500
            // use sampling of 8
            // if stars >= 500 && < 900
            // use sampling of 16
            // if stars > 900
            // use sampling of 24

            if (chosenStarCount < 100)
                return 3;
            if (chosenStarCount < 300)
                return 6;
            if (chosenStarCount < 500)
                return 8;
            if (chosenStarCount < 900)
                return 16;

            return 24;
        }

        /// <inheritdoc />
        public async Task<SolveResult> SolveFieldAsync(IImageDimensions imageDimensions, IList<ImageStar> stars, 
            ISearchStrategy strategy, SolverOptions options, CancellationToken cancellationToken)
        {

            if (strategy == null)
                throw new SolverException("Must define a search strategy");

            SolveResult result = null;
            
            _logger.Write("Image parsed, starting the solve");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Max stars:
            // Take minimum of 300 stars. 
            // Take maximum of 1200 stars.
            // If 0.33 * detected stars > 300, take that many.

            int maxStars;
            if (options?.UseMaxStars != null)
                maxStars = options.UseMaxStars.Value;
            else
                maxStars = 0.33 * stars.Count <= 300
                    ? 300
                    : (int)Math.Min(0.33 * stars.Count, 1200);
            

            var chosenDetectedStars = TakeBrightest(stars, maxStars);

            _logger.Write($"Chose {chosenDetectedStars.Count} stars from the detected stars for quad formation");
            IQuadDatabase quadDb;
            if (_quadDatabaseFactoryAsync != null)
                quadDb = await _quadDatabaseFactoryAsync.Invoke();
            else
                quadDb = _quadDatabaseFactory.Invoke(); 
            

            _tentativeMatches = 0;
            _iterations = 0;
            
            // Form quads from image stars
            var (imageStarQuads, countInFirstPass) = FormImageStarQuads(chosenDetectedStars.ToList()); 
            _logger.Write($"Formed {imageStarQuads.Length} quads from the chosen stars");

            var completionCts = new CancellationTokenSource();
            var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(completionCts.Token, cancellationToken);

            int? sampling = options.UseSampling;

            if(sampling != null && sampling > 1)
                _logger.Write($"Using sampling, 1/{sampling} quads will be used for the first search round");
            else if (sampling == null)
            {
                _logger.Write("Using auto-sampling");
                sampling = ApproximateGoodSamplingValue(chosenDetectedStars.Count);
            }

            var diagnosticsData = new SolveDiagnosticsData()
            {
                DetectedStars = stars,
                DetectedQuadDensity = countInFirstPass,
                MatchInstances = null,
                UsedStarCount = chosenDetectedStars.Count
            };

            diagnosticsData.FoundUsingRunType = sampling <= 1 ? SolveRunType.NonSampledRun : SolveRunType.SampledRun;

            if (strategy.UseParallelism)
            {
                Task<SolveResult[]> whenAllResult = null;

                _logger.Write("Starting search tasks in parallel");
                
                var searchTasks = strategy.GetSearchQueue().Select(searchRun => Task.Run(async () =>
                    await TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass, quadDb, options.UseSampling ?? 1, imageStarQuads,
                        completionCts)));


                try
                {
                    whenAllResult = Task.WhenAll(searchTasks);
                    await whenAllResult;

                    if (options.UseSampling != null && options.UseSampling > 1)
                    {
                        var resultSet = GetMatchedAndUnmatchedSearchRuns(whenAllResult.Result);
                        diagnosticsData.NumberOfPotentialHitAreasFound = resultSet.withMatches.Length;

                        if (resultSet.withMatches.FirstOrDefault(x => x != null && x.Success) == null && !combinedCts.Token.IsCancellationRequested)
                        {
                            diagnosticsData.FoundUsingRunType = SolveRunType.SampledHintedRun;
                            
                            // No result: run again, first on those that had potential matches, then if still no full result, on those that did not.
                            _logger.Write("Sampling: no result from the first search round, executing a second round without sampling on areas that had potential matches with sampling on");
                            var potentialMatchQueue = resultSet.withMatches.Select(x => x.SearchRun).ToArray();

                            var potentialMatchSearchTasks = potentialMatchQueue.Select(searchRun => Task.Run(async () =>
                                await TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass, quadDb, 1, imageStarQuads,
                                    completionCts)));

                            whenAllResult = Task.WhenAll(potentialMatchSearchTasks);
                            await whenAllResult;

                            if (whenAllResult.Result.FirstOrDefault(r => r != null && r.Success) == null && !combinedCts.Token.IsCancellationRequested)
                            {
                                diagnosticsData.FoundUsingRunType = SolveRunType.SampledLastDitchRun;
                                _logger.Write("Sampling: no result from the second search round, executing a third round without sampling on areas that had no matches with sampling on");
                                var lastDitchQueue = resultSet.withoutMatches.Select(x => x.SearchRun).ToArray();
                                var lastDitchSearchTasks = lastDitchQueue.Select(searchRun => Task.Run(async () =>
                                    await TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass, quadDb, 1, imageStarQuads,
                                        completionCts)));

                                whenAllResult = Task.WhenAll(lastDitchSearchTasks);
                                await whenAllResult;
                            }
                        }

                    }
                }
                finally
                {
                    stopwatch.Stop();
                    _logger.Write($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                    result = whenAllResult.Result.FirstOrDefault(r => r != null && r.Success) ?? new SolveResult();
                    diagnosticsData.MatchInstances = result.DiagnosticsData.MatchInstances;
                    result.DiagnosticsData = diagnosticsData;
                    result.TimeSpent = stopwatch.Elapsed;
                    result.AreasSearched = _iterations;
                    if (cancellationToken.IsCancellationRequested)
                        result.Canceled = true;
                }
            }
            else
            {
                _logger.Write("Starting search tasks in serial mode");

                var serialSearches = new List<SolveResult>();

                // For convenience.
                void MakeSuccessResult(SolveResult r)
                {
                    result = r;
                    diagnosticsData.MatchInstances = r.DiagnosticsData.MatchInstances;
                    result.DiagnosticsData = diagnosticsData;
                    result.TimeSpent = stopwatch.Elapsed;
                    result.AreasSearched = _iterations;
                }

                foreach (var searchRun in strategy.GetSearchQueue())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var taskResult = await TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass, quadDb, options.UseSampling ?? 1,
                        imageStarQuads,
                        completionCts);

                    serialSearches.Add(taskResult);

                    if (taskResult != null && taskResult.Success)
                    {
                        stopwatch.Stop();
                        _logger.Write($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                        MakeSuccessResult(taskResult);

                        break;
                    }
                    
                }

                if (options.UseSampling != null && options.UseSampling > 1)
                {
                    var resultSet = GetMatchedAndUnmatchedSearchRuns(serialSearches.ToArray());
                    diagnosticsData.NumberOfPotentialHitAreasFound = resultSet.withMatches.Length;

                    if (resultSet.withMatches.FirstOrDefault(x => x != null && x.Success) == null && !combinedCts.Token.IsCancellationRequested)
                    {
                        // No result: run again, first on those that had potential matches, then if still no full result, on those that did not.

                        _logger.Write("Sampling: no result from the first search round, executing a second round without sampling on areas that had potential matches with sampling on");
                        diagnosticsData.FoundUsingRunType = SolveRunType.SampledHintedRun;

                        var potentialMatchQueue = resultSet.withMatches.Select(x => x.SearchRun).ToArray();

                        foreach (var searchRun in potentialMatchQueue)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            var taskResult = await TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass, quadDb, 1,
                                imageStarQuads,
                                completionCts);

                            if (taskResult != null && taskResult.Success)
                            {
                                stopwatch.Stop();
                                _logger.Write($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                                MakeSuccessResult(taskResult);

                                break;
                            }
                        }
                        
                        if (result == null && !combinedCts.Token.IsCancellationRequested)
                        {
                            _logger.Write("Sampling: no result from the second search round, executing a third round without sampling on areas that had no matches with sampling on");
                            diagnosticsData.FoundUsingRunType = SolveRunType.SampledLastDitchRun;

                            var lastDitchQueue = resultSet.withoutMatches.Select(x => x.SearchRun).ToArray();
                            foreach (var searchRun in lastDitchQueue)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    break;

                                var taskResult = await TrySolveSingle(imageDimensions, combinedCts, searchRun, countInFirstPass, quadDb, 1,
                                    imageStarQuads,
                                    completionCts);

                                if (taskResult != null && taskResult.Success)
                                {
                                    stopwatch.Stop();
                                    _logger.Write($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                                    MakeSuccessResult(taskResult);

                                    break;
                                }
                            }
                        }
                    }

                }
                
                if (result == null)
                {
                    stopwatch.Stop();
                    _logger.Write($"Search tasks finished. Time spent: {stopwatch.Elapsed}");
                    result = new SolveResult();
                    result.DiagnosticsData = diagnosticsData;
                    result.AreasSearched = _iterations;
                    result.TimeSpent = stopwatch.Elapsed;
                    if (cancellationToken.IsCancellationRequested)
                        result.Canceled = true;
                }
            }


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
                
                if(results[i].HadPotentialMatches)
                    withMatches.Add(results[i]);
                else
                    withoutMatches.Add(results[i]);
            }

            return (withMatches.ToArray(), withoutMatches.ToArray());
        }

        private async Task<SolveResult> TrySolveSingle(IImageDimensions imageDimensions, CancellationTokenSource cancellationCts, SearchRun searchRun,
            int countInFirstPass, IQuadDatabase quadDb, int sampling, ImageStarQuad[] imageStarQuads, CancellationTokenSource completionCts)
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

            
            int minMatches = 5;

            List<StarQuadMatch> matchingQuads = null;

            var databaseQuads = await quadDb.GetQuadsAsync(searchRun.Center, searchRun.RadiusDegrees, (int) quadsPerSqDeg,
                searchRun.DensityOffsets, sampling, imageStarQuads);
            
            // For sampling logic.
            if(databaseQuads.Count > 0)
                taskResult.HadPotentialMatches = true;

            _logger.Write($"{logPrefix} {databaseQuads.Count} potential database matches");
            if (databaseQuads.Count < minMatches)
            {
                return taskResult;
                //return null;
            }

            // Found enough matches; a likely hit. If this was a sampled run, spend the time to retrieve the full quad set without sampling
            // as we're going to try for a solution.
            if(sampling > 1)
                databaseQuads = await quadDb.GetQuadsAsync(searchRun.Center, searchRun.RadiusDegrees, (int) quadsPerSqDeg,
                    searchRun.DensityOffsets, 1, imageStarQuads);

            matchingQuads = FindMatches(pixelAngularSearchFieldSizeRatio, imageStarQuads, databaseQuads, 0.01, minMatches);

            if (matchingQuads.Count >= minMatches)
            {
                _logger.Write($"{logPrefix} {matchingQuads.Count} image-catalog matches, attempting to calculate solution");
                var preliminarySolution = CalculateSolution(imageDimensions, matchingQuads, searchRun.Center);

                if (!IsValidSolution(preliminarySolution))
                {
                    _logger.Write($"{logPrefix} not a valid solution");
                    //return null;
                    return taskResult;
                }

                Interlocked.Increment(ref _tentativeMatches);

                // Probably off really badly, so don't accept it.
                if (preliminarySolution.Radius > 2 * searchRun.RadiusDegrees)
                {
                    return taskResult;
                    //return null;
                }

                pixelAngularSearchFieldSizeRatio = imageDiameterInPixels / preliminarySolution.Radius * 2;


                _logger.Write($"{logPrefix} valid solution, calculating an improved solution");
                // Calculate a second time; we may be quite a bit off if we're detecting the quads at an edge,
                // so calculating it a second time with the center and radius of the first solution
                // should improve our accuracy.
                var improvedSolution = await PerformAccuracyImprovementForSolution(imageDimensions, preliminarySolution,
                    pixelAngularSearchFieldSizeRatio, quadDb, imageStarQuads, (int) quadsPerSqDeg, minMatches);

                if (!IsValidSolution(improvedSolution.solution))
                {
                    _logger.Write($"{logPrefix} solution improve failed");
                    //return null;
                    return taskResult;
                }

                _logger.Write($"{logPrefix} valid solution was found");
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
            //return null;

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

        private async Task<(Solution solution, List<StarQuadMatch> matches)> PerformAccuracyImprovementForSolution(IImageDimensions imageDimensions, Solution solution,
            double pixelAngularSearchFieldSizeRatio,
            IQuadDatabase quadDatabase, ImageStarQuad[] imageStarQuads, int quadsPerSqDeg, int minMatches)
        {
            var resolvedCenter = solution.PlateCenter;
            var densityOffsets = new[] {-5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5}; // Include many, to improve the odds and to maximize match chances.
            var databaseQuads = await quadDatabase.GetQuadsAsync(resolvedCenter, solution.Radius, quadsPerSqDeg, densityOffsets, 1, imageStarQuads);
            var matchingQuads = FindMatches(pixelAngularSearchFieldSizeRatio, imageStarQuads, databaseQuads, 0.01, minMatches);
            if (matchingQuads.Count >= minMatches)
                return (CalculateSolution(imageDimensions, matchingQuads, resolvedCenter), matchingQuads);
            return (null, null);
        }

        private Solution CalculateSolution(IImageDimensions imageDimensions, List<StarQuadMatch> matches, EquatorialCoords scopeCoords)
        {
            var imgW = imageDimensions.ImageWidth;
            var imgH = imageDimensions.ImageHeight;
            var pc = SolvePlateConstants(matches, scopeCoords);

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

            var solution = new Solution(scopeCoords, imageCenterEquatorial, imgW, imgH, pixScale,
                fieldWidthDeg, fieldHeightDeg, fieldRadiusDeg, pc, cdelt1, cdelt2, crota1, crota2, cd1_1, cd2_1, cd1_2,
                cd2_2, scopeCoords.Ra, scopeCoords.Dec, scopePxX, scopePxY);

            
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
            var passes = 2;
            for (var p = 0; p < passes; p++)
            {
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

            var quadsArray = quads.Distinct(new StarQuad.StarQuadStarBasedEqualityComparer()).Cast<ImageStarQuad>().ToArray();
            //countInFirstPass = quadsArray.Length;
            return (quadsArray, countInFirstPass);
        }
    }
}