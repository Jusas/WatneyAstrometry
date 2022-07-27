using System;
using System.Collections.Generic;
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
    }
}
