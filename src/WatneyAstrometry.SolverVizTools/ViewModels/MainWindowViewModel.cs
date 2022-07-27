// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Services;
using IServiceProvider = WatneyAstrometry.SolverVizTools.Abstractions.IServiceProvider;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        //public ObservableCollection<SolveProfile> SolveProfiles { get; private set; }
        
        private bool _isPaneOpen = true;
        private readonly IServiceProvider _serviceProvider;

        public bool IsPaneOpen
        {
            get => _isPaneOpen;
            set => this.RaiseAndSetIfChanged(ref _isPaneOpen, value);
        }


        public SettingsManagerViewModel SettingsManagerViewModel { get; private set; }

        public SettingsPaneViewModel SettingsPaneViewModel { get; private set; }

        public SolveProcessViewModel SolveProcessViewModel { get; private set; }

        /// <summary>
        /// For designer only.
        /// </summary>
        public MainWindowViewModel()
        {
            PopulateInitialData();
        }

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            PopulateInitialData();
        }

        private void PopulateInitialData()
        {
        }

        protected override void OnViewCreated()
        {
            SettingsManagerViewModel = new SettingsManagerViewModel(_serviceProvider)
            {
                OwnerWindow = this.OwnerWindow
            };
            SettingsPaneViewModel = new SettingsPaneViewModel(_serviceProvider);
            SolveProcessViewModel = new SolveProcessViewModel(_serviceProvider)
            {
                OwnerWindow = this.OwnerWindow
            };

        }

        public void SetSolveSettingsPaneVisible()
        {
            IsPaneOpen = !IsPaneOpen;
        }
    }



}
