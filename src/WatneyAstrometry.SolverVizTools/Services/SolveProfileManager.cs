// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicData;
using WatneyAstrometry.SolverVizTools.Abstractions;
using WatneyAstrometry.SolverVizTools.Models.Profile;
using WatneyAstrometry.SolverVizTools.Utils;

namespace WatneyAstrometry.SolverVizTools.Services;

public class SolveProfileManager : ISolveProfileManager
{

    public class Options
    {
        public string StorageFolder { get; set; }
        public string ProfilesFileName { get; set; }
    }

    private readonly Options _options;
    public string ProfilesJsonFilename => Path.Combine(_options.StorageFolder, _options.ProfilesFileName);
    public ObservableCollection<SolveProfile> Profiles { get; } = new ObservableCollection<SolveProfile>();

    
    public static Options DefaultOptions => new Options()
    {
        StorageFolder = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), 
            ".watney", "solverviztools"),
        ProfilesFileName = "solverprofiles.json"
    };

    public SolveProfileManager()
    {
        _options = DefaultOptions;
    }

    public SolveProfileManager(Options options)
    {
        _options = options ?? DefaultOptions;
    }

    public SolveProfile CreateNewProfile(string name, SolveProfileType type)
    {
        if (Profiles.Any(x => x.Name == name && x.ProfileType == type))
            throw new SolveProfileException($"A profile with name '{name}' already exists");

        var profile = new SolveProfile
        {
            Name = name,
            ProfileType = type,
            BlindOptions = type == SolveProfileType.Blind ? new BlindOptions() : null,
            GenericOptions = new GenericOptions(),
            NearbyOptions = type == SolveProfileType.Nearby ? new NearbyOptions() : null,
            IsDeletable = true
        };
        Profiles.Add(profile);
        SaveProfiles();
        return profile;
    }
    

    public void SaveProfiles()
    {
        var serialized = JsonSerializer.Serialize(Profiles, new JsonSerializerOptions()
        {
            MaxDepth = 32,
            WriteIndented = true
        });

        var profileFullPath = Path.GetDirectoryName(ProfilesJsonFilename);
        Directory.CreateDirectory(profileFullPath);

        File.WriteAllText(ProfilesJsonFilename, serialized);
    }

    public void DeleteProfile(SolveProfile profile)
    {
        var match = Profiles.FirstOrDefault(x => x.Name == profile.Name && x.ProfileType == profile.ProfileType);
        if (match != null)
        {
            if (!match.IsDeletable)
                throw new SolveProfileException("Default profiles cannot be deleted");

            Profiles.Remove(match);
            SaveProfiles();
        }
    }

    public ObservableCollection<SolveProfile> GetProfiles(bool fromDisk, bool copy)
    {
        if (fromDisk)
        {
            if (File.Exists(ProfilesJsonFilename))
            {
                var profilesJson = File.ReadAllText(ProfilesJsonFilename);
                var profiles = JsonSerializer.Deserialize<List<SolveProfile>>(profilesJson);
                Profiles.Clear();
                Profiles.AddRange(profiles!);
            }

            EnsureDefaultProfilesExist();
        }

        if (copy)
        {
            var clonedCollection = new ObservableCollection<SolveProfile>();
            clonedCollection.AddRange(Profiles.Select(p => p.CloneInstance()));
            return clonedCollection;
        }

        return Profiles;
    }


    private void EnsureDefaultProfilesExist()
    {
        var name = "Default";

        var defaultBlindProfile = 
            Profiles.FirstOrDefault(x => x.Name == name && x.ProfileType == SolveProfileType.Blind);
        if (defaultBlindProfile == null)
        {
            defaultBlindProfile = new SolveProfile
            {
                Name = name,
                ProfileType = SolveProfileType.Blind,
                GenericOptions = new GenericOptions(),
                NearbyOptions = null,
                BlindOptions = new BlindOptions
                {
                    MinRadius = 0.25,
                    MaxRadius = 8,
                    SearchOrder = SearchOrder.NorthFirst
                },
                IsDeletable = false
            };
            Profiles.Add(defaultBlindProfile);
        }

        var defaultNearbyProfile =
            Profiles.FirstOrDefault(x => x.Name == name && x.ProfileType == SolveProfileType.Nearby);
        if (defaultNearbyProfile == null)
        {
            defaultNearbyProfile = new SolveProfile
            {
                Name = name,
                ProfileType = SolveProfileType.Nearby,
                GenericOptions = new GenericOptions
                {
                    Sampling = 0,
                    HigherDensityOffset = 2,
                    LowerDensityOffset = 2
                },
                BlindOptions = null,
                NearbyOptions = new NearbyOptions
                {
                    InputSource = InputSource.FitsHeaders,
                    SearchRadius = 10
                },
                IsDeletable = false
            };
            Profiles.Add(defaultNearbyProfile);
        }


    }
}