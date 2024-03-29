﻿// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace WatneyAstrometry.SolverVizTools.Models.Profile
{

    public enum SearchOrder
    {
        [Description("North first")]
        NorthFirst,

        [Description("South first")]
        SouthFirst
    }

    public class BlindOptions : ReactiveObject
    {
        public double? MinRadius { get; set; }
        public double? MaxRadius { get; set; }
        public SearchOrder SearchOrder { get; set; }
        
    }
}
