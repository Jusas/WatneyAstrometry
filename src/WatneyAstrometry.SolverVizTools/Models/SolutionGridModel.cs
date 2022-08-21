// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using WatneyAstrometry.Core.MathUtils;

namespace WatneyAstrometry.SolverVizTools.Models
{
    public class SolutionGridModel : ReactiveObject
    {
        private double _ra;
        private double _dec;
        private double _fieldRadius;
        private double _orientation;
        private string _parity;
        private int _starsDetected;
        private int _starsUsed;

        private double _starDetectionDuration;
        private double _solverDuration;
        private double _fullDuration;
        private int _matches;
        
        public string RaHms { get; set; }
        public double Ra
        {
            get => _ra;
            set
            {
                _ra = value;
                RaHms = Conversions.RaDegreesToHhMmSs(value);
                this.RaisePropertyChanged(nameof(Ra));
                this.RaisePropertyChanged(nameof(RaHms));
            }
        }


        public string DecDms { get; set; }
        public double Dec
        {
            get => _dec;
            set
            {
                _dec = value;
                DecDms = Conversions.DecDegreesToDdMmSs(value);
                this.RaisePropertyChanged(nameof(Dec));
                this.RaisePropertyChanged(nameof(DecDms));
            }
        }

        public double FieldRadius
        {
            get => _fieldRadius;
            set => this.RaiseAndSetIfChanged(ref _fieldRadius, value);
        }

        public double Orientation
        {
            get => _orientation;
            set => this.RaiseAndSetIfChanged(ref _orientation, value);
        }

        public string Parity
        {
            get => _parity;
            set => this.RaiseAndSetIfChanged(ref _parity, value);
        }

        public int StarsDetected
        {
            get => _starsDetected;
            set => this.RaiseAndSetIfChanged(ref _starsDetected, value);
        }

        public int StarsUsed
        {
            get => _starsUsed;
            set => this.RaiseAndSetIfChanged(ref _starsUsed, value);
        }

        public double StarDetectionDuration
        {
            get => _starDetectionDuration;
            set => this.RaiseAndSetIfChanged(ref _starDetectionDuration, value);
        }
        public double SolverDuration
        {
            get => _solverDuration;
            set => this.RaiseAndSetIfChanged(ref _solverDuration, value);
        }
        public double FullDuration
        {
            get => _fullDuration;
            set => this.RaiseAndSetIfChanged(ref _fullDuration, value);
        }
        public int Matches
        {
            get => _matches;
            set => this.RaiseAndSetIfChanged(ref _matches, value);
        }
    }
}
