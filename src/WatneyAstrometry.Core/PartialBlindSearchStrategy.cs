// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace WatneyAstrometry.Core
{
    /// <summary>
    /// A partial blind search strategy. Use <see cref="BlindSearchStrategy.Slice"/> to produce
    /// a bunch of these. They can be used for example to execute a single blind solve as parallel tasks, in multiple compute nodes.
    /// </summary>
    public class PartialBlindSearchStrategy : ISearchStrategy
    {
        /// <summary>
        /// The list of search runs this partial blind search strategy contains.
        /// </summary>
        public List<SearchRun> SearchRuns { get; set; } = new List<SearchRun>();

        /// <summary>
        /// Ctor
        /// </summary>
        public PartialBlindSearchStrategy()
        {
        }
        
        /// <summary>
        /// Returns the search queue (i.e. <see cref="SearchRuns"/>)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SearchRun> GetSearchQueue()
        {
            return SearchRuns;
        }

        /// <inheritdoc />
        public bool UseParallelism { get; set; }
    }
}