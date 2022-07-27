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
            private readonly bool _inverse;

            public ObjectRefEqualsConverter(bool inverse)
            {
                _inverse = inverse;
            }

            public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
            {
                var firstValue = values[0];
                var otherValues = values.Skip(1);

                if (_inverse)
                {
                    foreach (var value in otherValues)
                        if (value == firstValue)
                            return false;
                    return true;
                }
                
                foreach(var value in otherValues)
                    if(value != firstValue)
                        return false;
                return true;
            }
        }

        public static IMultiValueConverter ObjectRefEquals => new ObjectRefEqualsConverter(false);
        public static IMultiValueConverter ObjectRefNotEquals => new ObjectRefEqualsConverter(true);

    }
}
