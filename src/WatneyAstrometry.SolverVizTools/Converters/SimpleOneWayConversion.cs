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
        return _converter((TValue)value, (TParam)parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}