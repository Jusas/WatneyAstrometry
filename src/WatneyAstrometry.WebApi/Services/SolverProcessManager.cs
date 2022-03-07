// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WatneyAstrometry.Core;
using WatneyAstrometry.Core.QuadDb;
using WatneyAstrometry.Core.StarDetection;
using WatneyAstrometry.Core.Types;
using WatneyAstrometry.WebApi.Exceptions;
using WatneyAstrometry.WebApi.Models;
using WatneyAstrometry.WebApi.Models.Domain;
using WatneyAstrometry.WebApi.Repositories;

namespace WatneyAstrometry.WebApi.Services;

public class SolverProcessManager : ISolverProcessManager
{

    public class Configuration
    {
        public string QuadDatabasePath { get; set; }
        public int AllowedConcurrentSolves { get; set; } = 1;
        public double StarDetectionBgOffset { get; set; } = 3.0;
        public TimeSpan SolverTimeout { get; set; } = TimeSpan.FromMinutes(2);
    }

    private readonly IQueueManager _queueManager;
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<SolverProcessManager> _logger;
    private readonly Configuration _config;

    private List<Task> _solveTasks = new();
    private readonly object _mutex = new object();

    private ConcurrentDictionary<string, CancellationTokenSource> _jobCancellationTokens = new ();

    private CompactQuadDatabase _quadDatabase;
    
    public SolverProcessManager(IOptions<Configuration> config, IQueueManager queueManager, 
        IJobRepository jobRepository, ILogger<SolverProcessManager> logger, IHostApplicationLifetime appLifetime)
    {
        _queueManager = queueManager;
        _jobRepository = jobRepository;
        _logger = logger;
        _config = config.Value;
        queueManager.OnJobQueued += QueueManagerOnOnJobQueued;
        queueManager.OnJobCanceled += QueueManagerOnOnJobCanceled;

        appLifetime
            .ApplicationStopped
            .Register(CancelAll);
    }

    private void QueueManagerOnOnJobCanceled(string id)
    {
        if(_jobCancellationTokens.TryGetValue(id, out var tokenSource))
            tokenSource.Cancel();
    }

    private void QueueManagerOnOnJobQueued(string id)
    {
        StartProcessingOfAllQueuedItems();
    }

    private void CancelAll()
    {
        _jobCancellationTokens.Values.ToList().ForEach(x => x.Cancel());
    }

    private void StartProcessingOfAllQueuedItems()
    {
        _logger.LogTrace("A job was queued, triggering the processing/emptying of the queue");
        lock (_mutex)
        {
            var numberOfRunningTasks = _solveTasks.Count(x => !x.IsCompleted);
            var numberOfAvailableTasks = _config.AllowedConcurrentSolves - numberOfRunningTasks;

            if (numberOfAvailableTasks <= 0)
                return;

            _solveTasks.RemoveAll(x => x.IsCompleted);

            int tasksToSpawn = _queueManager.QueueSize > _config.AllowedConcurrentSolves
                ? numberOfAvailableTasks
                : _queueManager.QueueSize;
            
            if(_quadDatabase == null)
                _quadDatabase = new CompactQuadDatabase().UseDataSource(_config.QuadDatabasePath);

            _logger.LogTrace($"Spawning {tasksToSpawn} solve tasks");

            for (var i = 0; i < tasksToSpawn; i++)
            {
                Task solveTask = Task.Run(TakeFromQueueAndSolveUntilQueueEmpty);
                solveTask.ContinueWith(CleanupQuadDb);
                _solveTasks.Add(solveTask);
            }
        }
    }

    private void CleanupQuadDb(Task _)
    {
        lock (_mutex)
        {
            if (_quadDatabase == null)
                return;

            _logger.LogTrace("Releasing QuadDatabase resources");
            var numberOfRunningTasks = _solveTasks.Count(x => !x.IsCompleted);
            if (numberOfRunningTasks == 0)
            {
                var qdb = _quadDatabase;
                _quadDatabase = null;
                qdb.Dispose();
            }
        }
        
    }

    private async Task TakeFromQueueAndSolveUntilQueueEmpty()
    {
        string jobId = null;
        while ((jobId = _queueManager.Dequeue()) != null)
        {
            var job = await _jobRepository.Get(jobId).ConfigureAwait(false);
            if (job == null)
                continue;

            try
            {
                await SolveJob(job).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SetJobError(job);
                _logger.LogError(ex, "SolveJob threw an exception: " + ex.Message);
            }

        }

        _logger.LogTrace("The queue is empty, nothing to do");
    }

    private async Task SolveJob(JobModel job)
    {

        var timeoutCancellationTokenSource = new CancellationTokenSource(_config.SolverTimeout);
        //var cancellationTokenSource = new CancellationTokenSource();
        _jobCancellationTokens.TryAdd(job.Id, timeoutCancellationTokenSource);

        _logger.LogTrace($"Starting solver for job {job.Id}, using timeout of {_config.SolverTimeout}");

        job.Status = JobStatus.Solving;
        job.SolveStarted = DateTimeOffset.UtcNow;
        await _jobRepository.Update(job).ConfigureAwait(false);
        
        ISearchStrategy searchStrategy = null;
        int? maxStars = null;
        int? useSampling = null;

        if (job.Parameters.MaxStars > 0)
            maxStars = job.Parameters.MaxStars;
        if (job.Parameters.Sampling > 0)
            useSampling = job.Parameters.Sampling;

        if (job.Parameters.Mode == "blind" && job.Parameters.BlindParameters != null)
        {
            var p = job.Parameters.BlindParameters;
            searchStrategy = new BlindSearchStrategy(new BlindSearchStrategyOptions
            {
                UseParallelism = true, // todo is it? should we enforce this?
                SearchOrderDec = p.DecSearchOrder ?? BlindSearchStrategyOptions.DecSearchOrder.NorthFirst,
                SearchOrderRa = p.RaSearchOrder ?? BlindSearchStrategyOptions.RaSearchOrder.EastFirst,
                MaxNegativeDensityOffset = job.Parameters.LowerDensityOffset ?? 1,
                MaxPositiveDensityOffset = job.Parameters.HigherDensityOffset ?? 1,
                MinRadiusDegrees = (float)p.MinRadius,
                StartRadiusDegrees = (float)p.MaxRadius
            });

            _logger.LogTrace("Using blind strategy, with parameters: " + JsonConvert.SerializeObject(searchStrategy));
        }
        else if (job.Parameters.NearbyParameters != null)
        {
            var p = job.Parameters.NearbyParameters;
            searchStrategy = new NearbySearchStrategy(new EquatorialCoords(p.Ra.Value, p.Dec.Value),
                new NearbySearchStrategyOptions
                {
                    UseParallelism = false,
                    MaxNegativeDensityOffset = job.Parameters.LowerDensityOffset ?? 1,
                    MaxPositiveDensityOffset = job.Parameters.HigherDensityOffset ?? 1,
                    MaxFieldRadiusDegrees = p.MaxFieldRadius.Value,
                    MinFieldRadiusDegrees = p.MinFieldRadius.Value,
                    SearchAreaRadiusDegrees = p.SearchRadius.Value
                });

            _logger.LogTrace("Using nearby strategy, with parameters: " + JsonConvert.SerializeObject(searchStrategy));
        }
        else
        {
            throw new SolverProcessException("Job did not have parameters set");
        }


        var solverOptions = new SolverOptions()
        {
            UseMaxStars = maxStars,
            UseSampling = useSampling
        };
        var inputImageFrame = new ExtractedStarImage { ImageHeight = job.ImageHeight, ImageWidth = job.ImageWidth };

        _logger.LogTrace("Using solver options: " + JsonConvert.SerializeObject(solverOptions));

        if (_jobCancellationTokens[job.Id].IsCancellationRequested)
        {
            await SetJobCancelled(job, null);
        }

        try
        {
            var solver = new Solver()
                .UseStarDetector(new DefaultStarDetector(_config.StarDetectionBgOffset))
                .UseQuadDatabase(() => _quadDatabase);
            var solverResult = await solver.SolveFieldAsync(inputImageFrame, job.Stars, searchStrategy, solverOptions,
                timeoutCancellationTokenSource.Token).ConfigureAwait(false);

            if (solverResult.Canceled)
            {
                _logger.LogTrace($"Job {job.Id} was canceled");
                await SetJobCancelled(job, solverResult);
                return;
            }

            if (solverResult.Success)
            {
                _logger.LogTrace($"Job {job.Id} was successfully solved");
                await SetJobSuccessful(job, solverResult);
            }
            else
            {
                _logger.LogTrace($"Job {job.Id} failed to solve");
                await SetJobFailed(job, solverResult);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Solver SolveFieldAsync threw and exception: " + e.Message);
        }

        _jobCancellationTokens.TryRemove(job.Id, out var _);


    }

    private async Task SetJobFailed(JobModel job, SolveResult solverResult)
    {
        try
        {
            job.Status = JobStatus.Failure;
            job.StarsUsed = solverResult.StarsUsedInSolve;
            job.Solution = new JobSolutionProperties
            {
                TimeSpent = solverResult.TimeSpent.TotalSeconds,
                SearchIterations = solverResult.AreasSearched
            };

            await _jobRepository.Update(job);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to update job {job.Id} status to Failed: " + e.Message);
        }

    }

    private async Task SetJobSuccessful(JobModel job, SolveResult solverResult)
    {
        try
        {
            var s = solverResult.Solution;
            var f = s.FitsHeaders;
            job.StarsUsed = solverResult.StarsUsedInSolve;
            job.Solution = new JobSolutionProperties
            {
                Dec = s.PlateCenter.Dec,
                Ra = s.PlateCenter.Ra,
                FieldRadius = s.Radius,
                Orientation = s.Orientation,
                PixScale = s.PixelScale,
                QuadMatches = solverResult.MatchedQuads,
                SearchIterations = solverResult.AreasSearched,
                TimeSpent = solverResult.TimeSpent.TotalSeconds,
                Parity = s.Parity.ToString().ToLowerInvariant(),
                FitsWcs = new JobSolutionFitsWcs
                {
                    Cd1_1 = f.CD1_1,
                    Cd1_2 = f.CD1_2,
                    Cd2_1 = f.CD2_1,
                    Cd2_2 = f.CD2_2,
                    Cdelt1 = f.CDELT1,
                    Cdelt2 = f.CDELT2,
                    Crota1 = f.CROTA1,
                    Crota2 = f.CROTA2,
                    Crpix1 = f.CRPIX1,
                    Crpix2 = f.CRPIX2,
                    Crval1 = f.CRVAL1,
                    Crval2 = f.CRVAL2
                }
            };

            _logger.LogTrace($"Job {job.Id} solution: " + JsonConvert.SerializeObject(job.Solution));

            job.Status = JobStatus.Success;
            await _jobRepository.Update(job);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to update job {job.Id} status to Successful: " + e.Message);
        }
        
    }

    private async Task SetJobError(JobModel job)
    {
        try
        {
            _logger.LogTrace($"Job {job.Id} ended with an error");
            job.Status = JobStatus.Error;
            await _jobRepository.Update(job);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to update job {job.Id} status to Error: " + e.Message);
        }
    }

    private async Task SetJobCancelled(JobModel job, SolveResult solverResult)
    {
        try
        {
            job.Status = JobStatus.Canceled;
            job.StarsUsed = solverResult?.StarsUsedInSolve;
            await _jobRepository.Update(job);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to update job {job.Id} status to Canceled: " + e.Message);
        }
    }

    

}