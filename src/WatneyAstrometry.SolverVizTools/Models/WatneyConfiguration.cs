// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace WatneyAstrometry.SolverVizTools.Models;

public class WatneyConfiguration : ReactiveObject
{
    private string _quadDatabasePath;

    public string QuadDatabasePath
    {
        get => _quadDatabasePath;
        set => this.RaiseAndSetIfChanged(ref _quadDatabasePath, value);
    }

    [JsonIgnore]
    public bool IsValidQuadDatabasePath
    {
        get => Directory.Exists(_quadDatabasePath);
    }
}