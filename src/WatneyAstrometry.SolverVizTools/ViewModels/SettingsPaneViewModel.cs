// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Services;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{

    public class SettingsPaneViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;

        private SolveProfile _selectedPresetProfile;
        public SolveProfile SelectedPresetProfile
        {
            get => _selectedPresetProfile;
            set => this.RaiseAndSetIfChanged(ref _selectedPresetProfile, value);
        }

        public ObservableCollection<SolveProfile> SolveProfiles { get; private set; }
        
        public ObservableCollection<SearchOrder> SearchOrders { get; private set; } = new ObservableCollection<SearchOrder>
        {
            SearchOrder.NorthFirst,
            SearchOrder.SouthFirst
        };

        public ObservableCollection<InputSource> NearbyInputSources { get; private set; } = new ObservableCollection<InputSource>
        {
            InputSource.FitsHeaders,
            InputSource.Manual
        };

        private readonly ISolveSettingsManager _solveSettingsManager;
        
        /// <summary>
        /// For designer only.
        /// </summary>
        public SettingsPaneViewModel()
        {
            _solveSettingsManager = new MockSolverSettingsManager();
            PopulateInitialData();
        }

        public SettingsPaneViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _solveSettingsManager = serviceProvider.GetService<ISolveSettingsManager>();
            PopulateInitialData();
        }

        private void PopulateInitialData()
        {
            SolveProfiles = _solveSettingsManager.GetProfiles(true, true);
            SelectedPresetProfile = SolveProfiles.First();
        }
        

    }
}