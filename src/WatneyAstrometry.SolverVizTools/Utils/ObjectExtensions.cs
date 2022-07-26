// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using WatneyAstrometry.SolverVizTools.Models.Profile;

namespace WatneyAstrometry.SolverVizTools.Utils;

public static class ObjectExtensions
{
    public static T CloneInstance<T>(this T obj)
    {
        var conv = JsonSerializer.Serialize(obj, new JsonSerializerOptions { MaxDepth = 32 });
        return JsonSerializer.Deserialize<T>(conv);
    }
}