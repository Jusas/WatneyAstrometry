using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;

namespace WatneyAstrometry.SolverVizTools.Converters
{
    public static class ObjectConverters
    {

        public class ObjectRefEqualsConverter : IMultiValueConverter
        {
            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                var firstValue = values[0];
                foreach(var value in values)
                    if(value != firstValue)
                        return false;
                return true;
            }
        }

        public static IMultiValueConverter ObjectRefEquals => new ObjectRefEqualsConverter();

    }
}
