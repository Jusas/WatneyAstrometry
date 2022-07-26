// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatneyAstrometry.SolverVizTools.Models.Profile
{
    public class SearchOrderItem
    {
        public string Name { get; set; }
        public SearchOrder Value { get; set; }
        public override string ToString() => Name;
    }
}
