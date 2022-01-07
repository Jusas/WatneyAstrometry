// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using WatneyAstrometry.WebApi.Repositories;
using WatneyAstrometry.WebApi.Services;

namespace WatneyAstrometry.WebApi;

public static class ServiceRegistration
{
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
        });

        services.AddHttpClient();

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
