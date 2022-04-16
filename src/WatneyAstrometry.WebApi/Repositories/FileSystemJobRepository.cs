// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WatneyAstrometry.WebApi.Models;
using WatneyAstrometry.WebApi.Models.Domain;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Repositories;

/// <summary>
/// Job repository that is maintained in the file system (file based, persistent).
/// </summary>
internal class FileSystemJobRepository : IJobRepository
{

    public class Configuration
    {
        public string WorkDirectory { get; set; }
        public TimeSpan JobLifeTime { get; set; }
    }

    private static Configuration _config;
    private static CancellationTokenSource _cancellationTokenSource;
    private static Task _purgeTask;
    private ILogger<FileSystemJobRepository> _logger;

    public FileSystemJobRepository(ILogger<FileSystemJobRepository> logger, IOptions<Configuration> config,
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
    
    private static void PurgeOldJobs(ILogger logger)
    {
        logger.LogTrace("Purging old job files from disk");

        var now = DateTime.Now;
        if (!Directory.Exists(_config.WorkDirectory))
            return;

        var jobDirectories = Directory.GetDirectories(_config.WorkDirectory, "job_*");
        logger.LogTrace($"Found {jobDirectories.Length} job directories (job_*) from work directory");

        var exceptions = new List<Exception>();
        foreach (var jobDirectory in jobDirectories)
        {
            try
            {
                var jobFile = Path.Combine(jobDirectory, "job.json");
                var jobId = Path.GetFileName(jobDirectory).Substring("job_".Length);
                if (jobId.Length == 0)
                    continue;
                var lastUpdated = File.GetLastWriteTime(jobFile);
                if (lastUpdated + _config.JobLifeTime < now)
                {
                    try
                    {
                        logger.LogTrace($"Deleting old job {jobId}");
                        File.Delete(jobFile);
                        Directory.Delete(jobDirectory);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Failed to delete job file or directory for job {jobId}");
                    }
                }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }


        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }
    }

    private async Task PeriodicalPurgeTask()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                PurgeOldJobs(_logger);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occurred when purging old jobs: " + e.Message);
            }

            try
            {
                await Task.Delay(60_000, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Purge jobs task was canceled");
            }

        }
    }

    public Task Insert(JobModel job)
    {
        job.Updated = DateTimeOffset.UtcNow;

        var jobDirectory = Path.Combine(_config.WorkDirectory, $"job_{job.Id}_{job.NumericId}");
        if (Directory.Exists(jobDirectory))
            throw new Exception($"Conflict: directory {jobDirectory} exists already - is this job ID unique?");

        try
        {
            Directory.CreateDirectory(jobDirectory);
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogError(e, $"Could not create directory {jobDirectory} due to a security exception");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Could not create directory {jobDirectory} due to a an exception");
            throw;
        }

        var jobFile = Path.Combine(jobDirectory, "job.json");
        var bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(job));
        File.WriteAllBytes(jobFile, bytes);

        return Task.CompletedTask;

    }

    public Task Update(JobModel job)
    {
        var jobDirectory = Path.Combine(_config.WorkDirectory, $"job_{job.Id}_{job.NumericId}");
        if (!Directory.Exists(jobDirectory))
            throw new Exception($"Job directory {jobDirectory} does not exist, the job does not exist");
        
        var jobFile = Path.Combine(jobDirectory, "job.json");
        try
        {
            var bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(job));
            File.WriteAllBytes(jobFile, bytes);
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogError(e, $"Could not write to file {jobFile} due to a security exception");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Could not write to file {jobFile} due to a an exception");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task<JobModel> Get(string id)
    {
        // var jobDirectory = Path.Combine(_config.WorkDirectory, $"job_{id}");
        var directories = Directory.GetDirectories(_config.WorkDirectory, $"job_{id}_*");
        var jobDirectory = directories.FirstOrDefault();

        if (jobDirectory == null)
            return null;

        var jobFile = Path.Combine(jobDirectory, "job.json");
        if (!File.Exists(jobFile))
            return null;

        var jobBytes = File.ReadAllBytes(jobFile);
        return Task.FromResult(JsonConvert.DeserializeObject<JobModel>(Encoding.ASCII.GetString(jobBytes)));
    }

    public Task<JobModel> Get(int numericId)
    {
        var directories = Directory.GetDirectories(_config.WorkDirectory, $"job_*_{numericId}");
        var jobDirectory = directories.FirstOrDefault();

        if (jobDirectory == null)
            return null;

        var jobFile = Path.Combine(jobDirectory, "job.json");
        if (!File.Exists(jobFile))
            return null;

        var jobBytes = File.ReadAllBytes(jobFile);
        return Task.FromResult(JsonConvert.DeserializeObject<JobModel>(Encoding.ASCII.GetString(jobBytes)));
    }
}