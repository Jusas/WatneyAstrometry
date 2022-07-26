// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WatneyAstrometry.SolverVizTools.Converters;

public static class EnumConverters
{

    public static IValueConverter ValueEquals => new SimpleOneWayConversion<Enum, Enum, bool>((val, par) =>
    {
        if (val == null || par == null)
            return false;

        if (val.GetType() == par.GetType() && Enum.Equals(val, par))
            return true;
        return false;
    });
    
}