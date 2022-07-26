// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using ReactiveUI;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Models.Profile;

namespace WatneyAstrometry.SolverVizTools.ViewModels
{
    public class NewSolveProfileDialogViewModel : ViewModelBase
    {
        private readonly ISolveProfileManager _solveProfileManager;


        public ObservableCollection<string> Types { get; set; } = new ObservableCollection<string>()
        {
            BlindTypeString,
            NearbyTypeString
        };
        
        public const string BlindTypeString = "Blind solve";
        public const string NearbyTypeString = "Nearby solve";

        private string _profileName;
        public string ProfileName
        {
            get => _profileName;
            set => this.RaiseAndSetIfChanged(ref _profileName, value);
        }

        private string _profileType;
        public string ProfileType 
        {
            get => _profileType;
            set => this.RaiseAndSetIfChanged(ref _profileType, value);
        }

        private string _errorText;

        public string ErrorText
        {
            get => _errorText;
            set => this.RaiseAndSetIfChanged(ref _errorText, value);
        }

        public NewSolveProfileDialogViewModel()
        {
            Initialize();
        }
        
        public NewSolveProfileDialogViewModel(ISolveProfileManager solveProfileManager)
        {
            Initialize();
            _solveProfileManager = solveProfileManager;
            this.WhenAnyValue(x => x.ProfileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(Reset!);
        }

        private void Initialize()
        {
            ProfileType = Types[0];
        }

        public void CreateProfile()
        {
            if (!string.IsNullOrEmpty(ProfileType) && !string.IsNullOrEmpty(ProfileName))
            {
                var profiles = _solveProfileManager.GetProfiles(true, false);
                try
                {
                    var type = ProfileType == BlindTypeString ? SolveProfileType.Blind : SolveProfileType.Nearby;
                    var createdProfile = _solveProfileManager.CreateNewProfile(ProfileName, type);
                    OwnerWindow.Close(createdProfile);
                }
                catch (Exception e)
                {
                    ErrorText = $"Cannot create profile. {e.Message}";
                }
            }
            else
            {
                ErrorText = "Enter a valid name and profile type";
            }
        }

        public void CancelCreate()
        {
            OwnerWindow.Close(null);
        }

        private async void Reset(string s)
        {
            ErrorText = "";
        }
    }
}
