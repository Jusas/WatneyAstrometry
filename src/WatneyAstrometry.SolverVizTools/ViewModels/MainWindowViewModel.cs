// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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


        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
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
            //var v = FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).ProductVersion;
            var v = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName)?.ProductVersion ?? "v?";
            WindowTitle = $"Watney Astrometry Desktop ({v})";
        }

        protected override void OnViewCreated()
        {
            SettingsManagerViewModel = _serviceProvider.GetService<SettingsManagerViewModel>();
            SettingsManagerViewModel.OwnerWindow = this.OwnerWindow;

            SettingsPaneViewModel = _serviceProvider.GetService<SettingsPaneViewModel>();
            SettingsPaneViewModel.OwnerWindow = this.OwnerWindow;

            SolveProcessViewModel = _serviceProvider.GetService<SolveProcessViewModel>();
            SolveProcessViewModel.OwnerWindow = this.OwnerWindow;
        }

        public void OnClosing()
        {
            var settingsManager = _serviceProvider.GetService<ISolveSettingsManager>();
            settingsManager.SaveWatneyConfiguration();
            //settingsManager.SaveProfiles();
        }

        public void SetSolveSettingsPaneVisible()
        {
            IsPaneOpen = !IsPaneOpen;
        }
    }



}
