// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Threading;
using System.Threading.Tasks;

namespace WatneyAstrometry.Core.Threading
{
    /// <summary>
    /// This has been used to replace Task.Run() calls.
    /// The reason for this is to allow us to control the concurrency, of how many threads we want to
    /// have available for the Solver to use. The LimitedConcurrencyLevelTaskScheduler controls
    /// how many tasks are executed in parallel, and forms kind of a custom thread pool.
    /// </summary>
    internal static class WatneyTaskFactory
    {
        private static TaskFactory _taskFactory = null;

        public static void SetConcurrentTasks(int limit)
        {
            _taskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(limit)); 
        }

        public static TaskFactory Instance => _taskFactory 
            ?? (_taskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(Solver.SolverGlobalConfiguration.MaxThreads)));
    }
}