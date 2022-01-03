using WatneyAstrometry.WebApi.Repositories;
using WatneyAstrometry.WebApi.Services;

namespace WatneyAstrometry.WebApi;

public static class ServiceRegistration
{
    public static void AddSolverApiServices(this IServiceCollection services, IConfiguration configuration, WatneyApiConfiguration watneyApiConfiguration)
    {
        services.AddSingleton<IJobManager, JobManager>();
        services.AddSingleton<IQueueManager, InMemoryQueueManager>();
        services.AddSingleton<ISolverProcessManager, SolverProcessManager>();
        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        
        services.Configure<SolverProcessManager.Configuration>(config =>
        {
            config.AllowedConcurrentSolves = watneyApiConfiguration.AllowedConcurrentSolves;
            config.QuadDatabasePath = watneyApiConfiguration.QuadDatabasePath;
            config.SolverTimeout = watneyApiConfiguration.SolverTimeoutValue;
        });

        services.Configure<InMemoryJobRepository.Configuration>(config =>
        {
            config.JobLifeTime = watneyApiConfiguration.JobLifetime;
        });
    }

    public static void InitializeApiServices(IServiceProvider sp)
    {
        // Instantiate SolverProcessManager to start the background work watching.
        sp.GetService<ISolverProcessManager>();

        // Initialize JobRepository
        sp.GetService<IJobRepository>();
    }

    public static void ShutdownApiServices(IServiceProvider sp)
    {

    }
}
