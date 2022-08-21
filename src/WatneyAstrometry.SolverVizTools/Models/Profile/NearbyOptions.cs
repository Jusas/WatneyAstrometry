// Copyright (c) Jussi Saarivirta.
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

    public enum InputSource
    {
        [Description("FITS file headers")]
        FitsHeaders,

        [Description("Manually entered")]
        Manual
    }
    
    public enum FieldRadiusSource
    {
        [Description("Single value")]
        SingleValue,

        [Description("Min/max radius with n steps")]
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

        private FieldRadiusSource _fieldRadiusSource;
        public FieldRadiusSource FieldRadiusSource
        {
            get => _fieldRadiusSource;
            set => this.RaiseAndSetIfChanged(ref _fieldRadiusSource, value);
        }
        public double FieldRadius { get; set; } = 1;
        public double FieldRadiusMin { get; set; } = 1;
        public double FieldRadiusMax { get; set; } = 2;
        public int IntermediateFieldRadiusSteps { get; set; } = 1;
        public double SearchRadius { get; set; } = 10;
    }
}
