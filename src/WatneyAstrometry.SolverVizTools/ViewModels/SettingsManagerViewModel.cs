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
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Services;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{

    public class SettingsManagerViewModel : ViewModelBase
    {


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

        private readonly ISolveProfileManager _solveProfileManager;
        private readonly IViewProvider _viewProvider;
        
        /// <summary>
        /// For designer only.
        /// </summary>
        public SettingsManagerViewModel()
        {
            _solveProfileManager = new MockSolverProfileManager();
            PopulateInitialData();
        }

        public SettingsManagerViewModel(ISolveProfileManager solveProfileManager, IViewProvider viewProvider)
        {
            _solveProfileManager = solveProfileManager;
            _viewProvider = viewProvider;
            PopulateInitialData();
        }

        private void PopulateInitialData()
        {
            SolveProfiles = _solveProfileManager.GetProfiles(true, false);
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

        public void SaveCurrentProfile()
        {
            _solveProfileManager.SaveProfiles();
        }

        public void DeleteCurrentProfile()
        {
            if (SelectedProfile != null)
            {
                var profileName = SelectedProfile.Name;
                try
                {
                    _solveProfileManager.DeleteProfile(SelectedProfile);

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