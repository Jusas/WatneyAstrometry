// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.WebApi.Repositories;
using WatneyAstrometry.WebApi.Services;

namespace WatneyAstrometry.WebApi;

/// <summary>
/// Registering services for the API.
/// </summary>
internal static class ServiceRegistration
{
    /// <summary>
    /// Adds the services required by the solver setup.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <param name="watneyApiConfiguration"></param>
    public static void AddSolverApiServices(this IServiceCollection services, IConfiguration configuration, WatneyApiConfiguration watneyApiConfiguration)
    {
        services.AddSingleton<IJobManager, JobManager>();
        services.AddSingleton<ISolverProcessManager, SolverProcessManager>();

        if (watneyApiConfiguration.UsePersistency)
        {
            services.AddSingleton<IQueueManager, FileSystemQueueManager>();
            services.AddSingleton<IJobRepository, FileSystemJobRepository>();

            services.Configure<FileSystemQueueManager.Configuration>(config =>
            {
                config.WorkDirectory = watneyApiConfiguration.WorkDirectory;
            });
            services.Configure<FileSystemJobRepository.Configuration>(config =>
            {
                config.JobLifeTime = watneyApiConfiguration.JobLifetime;
                config.WorkDirectory = watneyApiConfiguration.WorkDirectory;
            });
        }
        else
        {
            services.AddSingleton<IQueueManager, InMemoryQueueManager>();
            services.AddSingleton<IJobRepository, InMemoryJobRepository>();
            
            services.Configure<InMemoryJobRepository.Configuration>(config =>
            {
                config.JobLifeTime = watneyApiConfiguration.JobLifetime;
            });
        }
        
        services.Configure<SolverProcessManager.Configuration>(config =>
        {
            config.AllowedConcurrentSolves = watneyApiConfiguration.AllowedConcurrentSolves;
            config.QuadDatabasePath = watneyApiConfiguration.QuadDatabasePath;
            config.SolverTimeout = watneyApiConfiguration.SolverTimeoutValue;
            // New config value, so we must also ensure we have a default value if it's left unconfigured.
            config.StarDetectionBgOffset = watneyApiConfiguration.StarDetectionBgOffset > 0 ? watneyApiConfiguration.StarDetectionBgOffset : 3.0;
        });

        services.AddHttpClient();

    }

    /// <summary>
    /// Starts initial services.
    /// </summary>
    /// <param name="sp"></param>
    public static void InitializeApiServices(IServiceProvider sp)
    {
        // Instantiate SolverProcessManager to start the background work watching.
        sp.GetService<ISolverProcessManager>();

        // Initialize JobRepository
        sp.GetService<IJobRepository>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sp"></param>
    public static void ShutdownApiServices(IServiceProvider sp)
    {

    }
}
