using Microsoft.Extensions.Options;

namespace WatneyAstrometry.WebApi.Services;

public class FileSystemQueueManager : IQueueManager
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
            if(first != null)
                File.WriteAllLines(_queueFile, allLines.Skip(1));
            return first;
        }
    }

    public void Cancel(string id)
    {
        lock (_mutex)
        {
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