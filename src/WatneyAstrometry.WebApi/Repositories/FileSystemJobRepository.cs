using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WatneyAstrometry.WebApi.Models;

namespace WatneyAstrometry.WebApi.Repositories;

public class FileSystemJobRepository : IJobRepository
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
        _purgeTask ??= Task.Run(PeriodicalPurgeTask);
    }

    public static void StopPeriodicalPurge()
    {
        _cancellationTokenSource.Cancel();
        _purgeTask.Wait(5000);
    }
    
    private static void PurgeOldJobs(ILogger logger)
    {
        var now = DateTime.Now;
        if (!Directory.Exists(_config.WorkDirectory))
            return;

        var jobDirectories = Directory.GetDirectories(_config.WorkDirectory, "job_*");
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

        if(exceptions.Any())
            throw new AggregateException(exceptions);
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

        var jobDirectory = Path.Combine(_config.WorkDirectory, $"job_{job.Id}");
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
        var jobDirectory = Path.Combine(_config.WorkDirectory, $"job_{job.Id}");
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
        var jobDirectory = Path.Combine(_config.WorkDirectory, $"job_{id}");
        if (!Directory.Exists(jobDirectory))
            return null;

        var jobFile = Path.Combine(jobDirectory, "job.json");
        if (!File.Exists(jobFile))
            return null;

        var jobBytes = File.ReadAllBytes(jobFile);
        return Task.FromResult(JsonConvert.DeserializeObject<JobModel>(Encoding.ASCII.GetString(jobBytes)));
    }


}