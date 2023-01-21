// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Exceptions;
using WatneyAstrometry.SolverVizTools.Models;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Services;
using IServiceProvider = WatneyAstrometry.SolverVizTools.Abstractions.IServiceProvider;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{

    public class SettingsManagerViewModel : ViewModelBase
    {
        
        private readonly IServiceProvider _serviceProvider;


        private SolveProfile _selectedProfile;
        public SolveProfile SelectedProfile
        {
            get => _selectedProfile;
            set => this.RaiseAndSetIfChanged(ref _selectedProfile, value);
        }

        public int LogicalCoreCount => Environment.ProcessorCount;

        public ObservableCollection<SolveProfile> SolveProfiles { get; private set; }

        public string CommandInfoText { get; set; }
        
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

        public ObservableCollection<FieldRadiusSource> FieldRadiusSources { get; private set; } = new ObservableCollection<FieldRadiusSource>
        {
            FieldRadiusSource.SingleValue,
            FieldRadiusSource.MinMaxWithSteps
        };

        public ObservableCollection<QuadDatabaseDataSet> DatabaseDataSets { get; set; }

        private WatneyConfiguration _watneyConfiguration;
        public WatneyConfiguration WatneyConfiguration
        {
            get => _watneyConfiguration;
            set => this.RaiseAndSetIfChanged(ref _watneyConfiguration, value);
        }

        private readonly ISolveSettingsManager _solveSettingsManager;
        private readonly IViewProvider _viewProvider;
        private readonly IDialogProvider _dialogProvider;
        private readonly IQuadDatabaseDownloadService _quadDatabaseDownloadService;

        private Dictionary<QuadDatabaseDataSet, CancellationTokenSource> _downloadCancellationTokenSources = new();

        /// <summary>
        /// For designer only.
        /// </summary>
        public SettingsManagerViewModel()
        {
            _solveSettingsManager = new MockSolverSettingsManager();
            _quadDatabaseDownloadService = new QuadDatabaseDownloadService();
            PopulateInitialData();

            
        }

        public SettingsManagerViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _solveSettingsManager = serviceProvider.GetService<ISolveSettingsManager>();
            _viewProvider = serviceProvider.GetService<IViewProvider>();
            _dialogProvider = serviceProvider.GetService<IDialogProvider>();
            _quadDatabaseDownloadService = serviceProvider.GetService<IQuadDatabaseDownloadService>();
            PopulateInitialData();

            this.WhenAnyValue(x => x.WatneyConfiguration.QuadDatabasePath)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(s => RefreshQuadDatabaseInfo(s));
        }

        private void PopulateInitialData()
        {
            SolveProfiles = _solveSettingsManager.GetProfiles(true, false);
            WatneyConfiguration = _solveSettingsManager.GetWatneyConfiguration(true, false);
            SelectedProfile = SolveProfiles.First();
            _quadDatabaseDownloadService.SetDatabaseDirectory(WatneyConfiguration.QuadDatabasePath);
            DatabaseDataSets =
                new ObservableCollection<QuadDatabaseDataSet>(_quadDatabaseDownloadService
                    .DownloadableQuadDatabaseDataSets);
        }

        public async Task OpenNewProfileDialog()
        {
            var dialog = _viewProvider.Instantiate<NewSolveProfileDialogViewModel>();
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var profile = await dialog.ShowDialog<SolveProfile>(OwnerWindow);
            if(profile != null)
                SelectedProfile = profile;
        }

        public async Task OpenBrowseWatneyDatabaseFolderDialog()
        {
            var folder = await _dialogProvider.ShowOpenFolderDialog(OwnerWindow, "Select Watney quad database directory",
                WatneyConfiguration.QuadDatabasePath);
            if(!string.IsNullOrEmpty(folder))
            {
                WatneyConfiguration.QuadDatabasePath = folder;
                this.RaisePropertyChanged(nameof(WatneyConfiguration));
                _solveSettingsManager.SaveWatneyConfiguration();
                _quadDatabaseDownloadService.SetDatabaseDirectory(folder);
            }
        }

        public void ResetThreadLimit()
        {
            WatneyConfiguration.LimitThreads = Environment.ProcessorCount - 1;
            this.RaisePropertyChanged(nameof(WatneyConfiguration));
            _solveSettingsManager.SaveWatneyConfiguration();
        }

        private void RefreshQuadDatabaseInfo(string path)
        {
            _quadDatabaseDownloadService.SetDatabaseDirectory(path);
            
        }



        public void SaveCurrentProfile()
        {
            _solveSettingsManager.SaveProfiles();

            var (name, type) = (_selectedProfile.Name, _selectedProfile.ProfileType);
            
            RefreshSettingsPaneProfiles();

            SelectedProfile = SolveProfiles.First(x => x.Name == name && x.ProfileType == type);
        }

        private void RefreshSettingsPaneProfiles()
        {

            var settingsPane = _serviceProvider.GetService<SettingsPaneViewModel>();
            settingsPane?.RefreshProfiles();
        }

        public void DeleteCurrentProfile()
        {
            if (SelectedProfile != null)
            {
                var profileName = SelectedProfile.Name;
                try
                {
                    _solveSettingsManager.DeleteProfile(SelectedProfile);

                    CommandInfoText = $"Deleted profile '{profileName}'";
                    this.RaisePropertyChanged(nameof(CommandInfoText));

                    RefreshSettingsPaneProfiles();
                    SelectedProfile = SolveProfiles.First();

                }
                catch (SolveProfileException e)
                {
                    CommandInfoText = $"Could not delete '{profileName}': {e.Message}";
                    this.RaisePropertyChanged(nameof(CommandInfoText));
                }
            }
        }

        public async Task DownloadDataSet(QuadDatabaseDataSet dataSet)
        {
            // Skip possible double download initiations
            if (_downloadCancellationTokenSources.ContainsKey(dataSet))
                return;

            var cts = new CancellationTokenSource();
            _downloadCancellationTokenSources[dataSet] = cts;

            if (WatneyConfiguration.QuadDatabasePath != _quadDatabaseDownloadService.DatabaseDir)
                _quadDatabaseDownloadService.SetDatabaseDirectory(WatneyConfiguration.QuadDatabasePath);

            
            try
            {
                await _quadDatabaseDownloadService.DownloadDataSet(dataSet, cts.Token);
            }
            catch (Exception e)
            {
                await _dialogProvider.ShowMessageBox(OwnerWindow, "Error", e.Message, DialogIcon.Error);
            }

            _downloadCancellationTokenSources.Remove(dataSet);


        }

        public async Task CancelDownloadingDataSet(QuadDatabaseDataSet dataSet)
        {
            _downloadCancellationTokenSources[dataSet].Cancel();
            _downloadCancellationTokenSources.Remove(dataSet);
        }

    }
}