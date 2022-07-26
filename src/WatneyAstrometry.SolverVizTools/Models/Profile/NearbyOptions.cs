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

    public enum InputSource
    {
        FitsHeaders,
        Manual
    }

    public enum FieldRadiusSource
    {
        SingleValue,
        MinMaxWithSteps
    }

    public class NearbyOptions : ReactiveObject
    {
        private InputSource _inputSource = InputSource.FitsHeaders;

        public InputSource InputSource
        {
            get => _inputSource;
            set => this.RaiseAndSetIfChanged(ref _inputSource, value);
        }
        public string Ra { get; set; } 
        public string Dec { get; set; }

        public FieldRadiusSource FieldRadiusSource { get; set; } = FieldRadiusSource.SingleValue;
        public double FieldRadius { get; set; } = 1;
        public double FieldRadiusMin { get; set; }
        public double FieldRadiusMax { get; set; }
        public int IntermediateFieldRadiusSteps { get; set; }
        public double SearchRadius { get; set; } = 10;
    }
}
