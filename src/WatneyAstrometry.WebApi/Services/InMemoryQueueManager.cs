// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Concurrent;
#pragma warning disable CS1591

namespace WatneyAstrometry.WebApi.Services;

/// <summary>
/// Queue manager for the in-memory queue.
/// </summary>
internal class InMemoryQueueManager : IQueueManager
{
    private readonly ILogger<InMemoryQueueManager> _logger;
    public event IQueueManager.JobQueuedHandler OnJobQueued;
    public event IQueueManager.JobCanceledHandler OnJobCanceled;

    private Queue<string> _queue = new();
    private readonly object _mutex = new();

    public int QueueSize => _queue.Count;

    public InMemoryQueueManager(ILogger<InMemoryQueueManager> logger)
    {
        _logger = logger;
    }

    public void Enqueue(string id)
    {
        lock (_mutex)
        {
            _logger.LogTrace($"Queuing job {id}");
            _queue.Enqueue(id);
        }
        OnJobQueued?.Invoke(id);
    }

    public string Dequeue()
    {
        lock (_mutex)
        {
            var didDequeue = _queue.TryDequeue(out var job);
            if (didDequeue)
            {
                _logger.LogTrace($"Dequeued job {job}");
                return job;
            }

            return null;
        }
        
    }

    public void Cancel(string id)
    {
        lock (_mutex)
        {
            _logger.LogTrace($"Cancelling job {id}, removing it from queue");
            _queue = new Queue<string>(_queue.Where(x => x != id));
        }
        OnJobCanceled?.Invoke(id);
    }
}