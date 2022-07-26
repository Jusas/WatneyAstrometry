// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatneyAstrometry.SolverVizTools.Models.Profile;

namespace WatneyAstrometry.SolverVizTools.Abstractions
{
    public interface ISolveProfileManager
    {
        SolveProfile CreateNewProfile(string name, SolveProfileType type);
        void SaveProfiles();
        void DeleteProfile(SolveProfile profile);
        ObservableCollection<SolveProfile> GetProfiles(bool fromDisk, bool copy);
    }
}
