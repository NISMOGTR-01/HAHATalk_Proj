using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Windows.Media;
using System.Globalization;
using System.Text;
using System.Windows.Data;


namespace WPFLib.Converters
{
    public class ValidatingBorderBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool? isValidation = values[0] as bool?;
            if(isValidation != null && isValidation == true)
            {
                return Brushes.Red;
            }
            else
            {
                return (Brush)values[1];
            }            
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
