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

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        //public ObservableCollection<SolveProfile> SolveProfiles { get; private set; }
        
        private bool _isPaneOpen = true;
        private readonly ISolveProfileManager _solveProfileManager;
        private readonly IViewProvider _viewProvider;

        public bool IsPaneOpen
        {
            get => _isPaneOpen;
            set => this.RaiseAndSetIfChanged(ref _isPaneOpen, value);
        }


        public SettingsManagerViewModel SettingsManagerViewModel { get; private set; }

        public SettingsPaneViewModel SettingsPaneViewModel { get; private set; }

        /// <summary>
        /// For designer only.
        /// </summary>
        public MainWindowViewModel()
        {
            _solveProfileManager = new MockSolverProfileManager();
            PopulateInitialData();
        }

        public MainWindowViewModel(ISolveProfileManager solveProfileManager, IViewProvider viewProvider)
        {
            _solveProfileManager = solveProfileManager;
            _viewProvider = viewProvider;
            PopulateInitialData();
        }

        private void PopulateInitialData()
        {
            // SolveProfiles = _solveProfileManager.GetProfiles(true, true);
        }

        protected override void OnViewCreated()
        {
            SettingsManagerViewModel = new SettingsManagerViewModel(_solveProfileManager, _viewProvider)
            {
                OwnerWindow = this.OwnerWindow
            };
            SettingsPaneViewModel = new SettingsPaneViewModel(_solveProfileManager);
        }

        public void SetSolveSettingsPaneVisible()
        {
            IsPaneOpen = !IsPaneOpen;
        }
    }



}
