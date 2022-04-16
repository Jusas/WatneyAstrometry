// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

#pragma warning disable CS1591
namespace WatneyAstrometry.WebApi.Services;

/// <summary>
/// Manages the job queue.
/// </summary>
public interface IQueueManager
{
    int QueueSize { get; }
    void Enqueue(string id);
    string Dequeue();
    void Cancel(string id);

    delegate void JobQueuedHandler(string id);
    delegate void JobCanceledHandler(string id);
    event JobQueuedHandler OnJobQueued;
    event JobCanceledHandler OnJobCanceled; // if in queue, we can just remove it from queue. If not in queue anymore, we invoke the evt handler.
}