// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WatneyAstrometry.SolverVizTools.Converters;

public class SimpleOneWayConversion<TValue, TParam, TOut> : IValueConverter
{

    private Func<TValue, TParam, TOut> _converter;

    public SimpleOneWayConversion(Func<TValue, TParam, TOut> converterFunc)
    {
        _converter = converterFunc;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(parameter is string s && typeof(TParam).IsPrimitive)
            parameter = ConvertStringParameterToPrimitive(parameter);

        return _converter((TValue)value, (TParam)parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
    
    private object ConvertStringParameterToPrimitive(object parameter)
    {
        var s = (string)parameter;
        var t = typeof(TParam);

        if (t == typeof(int))
            return int.Parse(s);
        if (t == typeof(long))
            return long.Parse(s);
        if (t == typeof(bool))
            return bool.Parse(s);
        if (t == typeof(double))
            return double.Parse(s, CultureInfo.InvariantCulture);
        if (t == typeof(float))
            return float.Parse(s, CultureInfo.InvariantCulture);

        return null;
    }
}