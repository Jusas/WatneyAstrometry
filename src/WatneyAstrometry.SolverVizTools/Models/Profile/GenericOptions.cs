// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace WatneyAstrometry.SolverVizTools.Models.Profile
{
    public class GenericOptions : ReactiveObject
    {
        public int MaxStars { get; set; } = 300;
        public int Sampling { get; set; } = 16;
        public uint? LowerDensityOffset { get; set; } = 1;
        public uint? HigherDensityOffset { get; set; } = 1;
        public bool UseParallelism { get; set; } = true;

    }
}
