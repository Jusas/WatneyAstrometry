// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using WatneyAstrometry.WebApi.Models;
using WatneyAstrometry.WebApi.Models.Domain;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Repositories;

/// <summary>
/// A job repository that keeps all jobs in memory and will be cleared
/// if the process restarts.
/// </summary>
internal class InMemoryJobRepository : IJobRepository
{
    private readonly ILogger<InMemoryJobRepository> _logger;

    public class Configuration
    {
        /// <summary>
        /// Jobs may only live this long untouched. Whenever a job is updated,
        /// it's touched. When it sits untouched longer than this TimeSpan, it's going to
        /// get purged to stop the memory from being filled completely by old jobs.
        /// </summary>
        public TimeSpan JobLifeTime { get; set; }
    }

    private static readonly ConcurrentDictionary<string, JobModel> _jobs = new();
    private static Configuration _config;
    private static CancellationTokenSource _cancellationTokenSource;
    private static Task _purgeTask;

    public InMemoryJobRepository(IOptions<Configuration> config, ILogger<InMemoryJobRepository> logger,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _config ??= config.Value;
        _cancellationTokenSource ??= new CancellationTokenSource();

        StartPeriodicalPurge();

        appLifetime
            .ApplicationStopped
            .Register(StopPeriodicalPurge);
    }

    public void StartPeriodicalPurge()
    {
        _logger.LogTrace("Registering a periodical purge task to clean up old jobs");
        _purgeTask ??= Task.Run(PeriodicalPurgeTask);
    }

    public static void StopPeriodicalPurge()
    {
        _cancellationTokenSource.Cancel();
        _purgeTask.Wait(5000);
    }
    
    public Task Insert(JobModel job)
    {
        job.Updated = DateTimeOffset.UtcNow;
        _jobs.TryAdd(job.Id, job);
        return Task.CompletedTask;
    }

    public Task Update(JobModel job)
    {
        job.Updated = DateTimeOffset.UtcNow;
        _jobs.AddOrUpdate(job.Id, job, (_, _) => job);
        return Task.CompletedTask;
    }

    public Task<JobModel> Get(string id)
    {
        return Task.FromResult(_jobs.TryGetValue(id, out JobModel job) ? job : null);

    }

    public Task<JobModel> Get(int numericId)
    {
        var match = _jobs
            .Where(x => x.Value.NumericId == numericId)
            .Select(x => x.Value)
            .FirstOrDefault();
        return Task.FromResult(match);
    }

    private static void PurgeOldJobs(ILogger logger)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredJobIds = _jobs.Where(x => x.Value.Updated + _config.JobLifeTime < now)
            .Select(x => x.Key).ToArray();
        if (expiredJobIds.Length > 0)
        {
            logger.LogTrace("Purging old jobs from memory");
            logger.LogTrace($"Removing {expiredJobIds.Length} old jobs");
        }
        foreach (var id in expiredJobIds)
            _jobs.TryRemove(id, out var _);
    }

    private async Task PeriodicalPurgeTask()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                PurgeOldJobs(_logger);
                await Task.Delay(60_000, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Purge jobs task was canceled");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occurred when purging old jobs: " + e.Message);
            }
        }
    }
    
}