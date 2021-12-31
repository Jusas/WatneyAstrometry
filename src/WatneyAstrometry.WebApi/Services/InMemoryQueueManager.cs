using System.Collections.Concurrent;

namespace WatneyAstrometry.WebApi.Services;

public class InMemoryQueueManager : IQueueManager
{
    public event IQueueManager.JobQueuedHandler OnJobQueued;
    public event IQueueManager.JobCanceledHandler OnJobCanceled;

    private Queue<string> _queue = new();
    private readonly object _mutex = new();

    public int QueueSize => _queue.Count;

    public void Enqueue(string id)
    {
        lock (_mutex)
        {
            _queue.Enqueue(id);
        }
        OnJobQueued?.Invoke(id);
    }

    public string Dequeue()
    {
        lock (_mutex)
        {
            return _queue.TryDequeue(out var item) ? item : null;
        }
        
    }

    public void Cancel(string id)
    {
        lock (_mutex)
        {
            _queue = new Queue<string>(_queue.Where(x => x != id));
        }
        OnJobCanceled?.Invoke(id);
    }
}