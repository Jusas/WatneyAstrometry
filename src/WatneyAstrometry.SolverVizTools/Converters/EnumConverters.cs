// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
    
    public static IValueConverter EnumDescription => new SimpleOneWayConversion<Enum, object, string>((val, par) =>
    {
        if (val == null)
            return "";

        var enumType = val.GetType();
        var memberInfo = enumType.GetMember(val.ToString());

        if (memberInfo.Length == 0)
            return val.ToString();
        
        var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
        var desc = attrs.FirstOrDefault();
        if (desc != null)
            return (desc as DescriptionAttribute).Description;

        return val.ToString();
        
    });
}