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
using WatneyAstrometry.SolverVizTools.Exceptions;
using WatneyAstrometry.SolverVizTools.Models;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Services;

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

        public WatneyConfiguration WatneyConfiguration { get; private set; }

        private readonly ISolveSettingsManager _solveSettingsManager;
        private readonly IViewProvider _viewProvider;
        private readonly IDialogProvider _dialogProvider;

        /// <summary>
        /// For designer only.
        /// </summary>
        public SettingsManagerViewModel()
        {
            _solveSettingsManager = new MockSolverSettingsManager();
            PopulateInitialData();
        }

        public SettingsManagerViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _solveSettingsManager = serviceProvider.GetService<ISolveSettingsManager>();
            _viewProvider = serviceProvider.GetService<IViewProvider>();
            _dialogProvider = serviceProvider.GetService<IDialogProvider>();
            PopulateInitialData();
        }

        private void PopulateInitialData()
        {
            SolveProfiles = _solveSettingsManager.GetProfiles(true, false);
            WatneyConfiguration = _solveSettingsManager.GetWatneyConfiguration(true, false);
            SelectedProfile = SolveProfiles.First();
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
            }
        }
        

        public void SaveCurrentProfile()
        {
            _solveSettingsManager.SaveProfiles();
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
                }
                catch (SolveProfileException e)
                {
                    CommandInfoText = $"Could not delete '{profileName}': {e.Message}";
                    this.RaisePropertyChanged(nameof(CommandInfoText));
                }
            }
        }

    }
}