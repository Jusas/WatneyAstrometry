// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.Options;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Services;

/// <summary>
/// File system based queue manager.
/// The queue is saved into a file.
/// </summary>
internal class FileSystemQueueManager : IQueueManager
{

    public class Configuration
    {
        public string WorkDirectory { get; set; }
    }

    public int QueueSize
    {
        get
        {
            if (!File.Exists(_queueFile))
                return 0;

            // The lazy way, but should be good enough for our purposes.
            return File.ReadAllLines(_queueFile).Length;
        }
    }

    private Configuration _configuration;
    private readonly object _mutex = new object();
    private readonly ILogger<FileSystemQueueManager> _logger;

    private string _queueFile => Path.Combine(_configuration.WorkDirectory, "queue.txt");

    public FileSystemQueueManager(ILogger<FileSystemQueueManager> logger, IOptions<Configuration> configuration)
    {
        _logger = logger;
        _configuration = configuration.Value;
    }

    private void InitializeQueueFile()
    {
        if (File.Exists(_queueFile))
            return;
        try
        {
            _logger.LogTrace($"Creating queue file {_queueFile}");
            File.Create(_queueFile).Dispose();
        }
        catch (UnauthorizedAccessException e)
        {
            _logger.LogError(e, $"Could not create queue file {_queueFile} due to a security exception");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Could not create queue file {_queueFile} due to an exception");
            throw;
        }
    }

    public void Enqueue(string id)
    {
        lock (_mutex)
        {
            _logger.LogTrace($"Queuing job {id}");
            InitializeQueueFile();
            File.AppendAllLines(_queueFile, new[] { id });
        }
        OnJobQueued?.Invoke(id);
    }

    public string Dequeue()
    {
        lock (_mutex)
        {
            InitializeQueueFile();
            var allLines = File.ReadAllLines(_queueFile);
            var first = allLines.FirstOrDefault();
            if (first != null)
            {
                _logger.LogTrace($"Dequeued job {first}");
                File.WriteAllLines(_queueFile, allLines.Skip(1));
            }

            return first;
        }
    }

    public void Cancel(string id)
    {
        lock (_mutex)
        {
            _logger.LogTrace($"Cancelling job {id}, removing it from queue");
            InitializeQueueFile();
            var allLines = File.ReadAllLines(_queueFile);
            var match = allLines.FirstOrDefault(x => x == id);
            if (match != null)
            {
                var newQueue = allLines.Where(x => x != id);
                File.WriteAllLines(_queueFile, newQueue);
            }
        }
        OnJobCanceled?.Invoke(id);
    }

    public event IQueueManager.JobQueuedHandler OnJobQueued;
    public event IQueueManager.JobCanceledHandler OnJobCanceled;
}