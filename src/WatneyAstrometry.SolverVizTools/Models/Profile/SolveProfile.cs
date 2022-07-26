// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ReactiveUI;

namespace WatneyAstrometry.SolverVizTools.Models.Profile
{
    public class SolveProfile : ReactiveObject
    {
        public string Name { get; set; }
        public bool IsDeletable { get; set; }
        public SolveProfileType ProfileType { get; set; }
        public GenericOptions GenericOptions { get; set; } = new();
        public NearbyOptions NearbyOptions { get; set; } = new();
        public BlindOptions BlindOptions { get; set; } = new();

        public override string ToString()
        {
            var typeString = ProfileType == SolveProfileType.Blind ? "blind solve" : "nearby solve";
            return $"{Name} ({typeString})";
        }
        
    }
}
