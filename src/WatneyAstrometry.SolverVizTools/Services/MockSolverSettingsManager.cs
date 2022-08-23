// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Models;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Utils;

namespace WatneyAstrometry.SolverVizTools.Services;

public class MockSolverSettingsManager : ISolveSettingsManager
{

    private ObservableCollection<SolveProfile> _profiles = new ObservableCollection<SolveProfile>();

    public MockSolverSettingsManager()
    {
        _profiles.Add(new SolveProfile
        {
            Name = "Default",
            ProfileType = SolveProfileType.Blind,
            IsDeletable = false
        });
        _profiles.Add(new SolveProfile
        {
            Name = "Default",
            ProfileType = SolveProfileType.Nearby,
            IsDeletable = false
        });
    }

    public SolveProfile CreateNewProfile(string name, SolveProfileType type)
    {
        var profile = new SolveProfile()
        {
            Name = name,
            ProfileType = type,
            IsDeletable = true
        };
        _profiles.Add(profile);
        return profile;
    }

    public void SaveProfiles()
    {
    }

    public void DeleteProfile(SolveProfile profile)
    {
        var match = _profiles.FirstOrDefault(x => x.Name == profile.Name && x.ProfileType == profile.ProfileType);
        if(match != null)
            _profiles.Remove(match);
    }

    public ObservableCollection<SolveProfile> GetProfiles(bool fromDisk, bool copy)
    {
        if (copy)
        {
            var cloneCollection = new ObservableCollection<SolveProfile>();
            cloneCollection.AddRange(_profiles.Select(p => p.CloneInstance()));
            return cloneCollection;
        }
        return _profiles;
    }

    public WatneyConfiguration GetWatneyConfiguration(bool fromDisk, bool copy)
    {
        return new WatneyConfiguration()
        {
            QuadDatabasePath = @"C:\temp"
        };
    }

    public void SaveWatneyConfiguration()
    {
    }

    public void LoadStoredGeneralSettings()
    {
    }

    public string GetStoredGeneralSetting(string settingName)
    {
        return null;
    }

    public void SetStoredGeneralSetting(string settingName, string value)
    {
    }

    public void SaveStoredGeneralSettings()
    {
    }
}