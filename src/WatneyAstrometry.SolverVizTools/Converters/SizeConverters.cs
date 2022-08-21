// Copyright (c) Jussi Saarivirta.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;

namespace WatneyAstrometry.SolverVizTools.Converters
{
    public static class SizeConverters
    {
        public static IValueConverter PercentageOf => new SimpleOneWayConversion<double, double, double>((val, par) =>
        {
            return val * par / 100.0;
        });

        public static MultiplyValuesConverter Multiply => new MultiplyValuesConverter();

        public class MultiplyValuesConverter : IMultiValueConverter
        {
            public MultiplyValuesConverter()
            {
            }

            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                var firstValue = values[0];
                var otherValues = values.Skip(1);

                try
                {
                    if (firstValue is double d)
                    {
                        foreach (var otherValue in otherValues.Cast<double>())
                        {
                            d *= otherValue;
                        }

                        return d;
                    }

                    if (firstValue is int i)
                    {
                        foreach (var otherValue in otherValues.Cast<int>())
                        {
                            i *= otherValue;
                        }

                        return i;
                    }
                }
                catch (Exception)
                {

                }

                return null;
            }
        }
    }
}
