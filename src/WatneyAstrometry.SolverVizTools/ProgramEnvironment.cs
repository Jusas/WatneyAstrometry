// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatneyAstrometry.SolverVizTools
{
    public static class ProgramEnvironment
    {
        public static string ApplicationDataFolder => Path.Combine(
            Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".watney", "solverviztools");
    }
}
