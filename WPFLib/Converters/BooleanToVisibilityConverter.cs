using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace WPFLib.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        // Convert : ViewModel -> View (XAML) 
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value가 bool타입인지 확인 (is) 
            if (value is bool bValue)
            {
                // true -> 보인다 
                // false -> 영역까지 삭제 (Collapsed) 
                return bValue ? Visibility.Visible : Visibility.Collapsed;
            }

            // bool 이 아니거나 예상치 못한 값은 숨김 처리 
            return Visibility.Collapsed;
        }

        // ConvertBack : View(XMAL) -> ViewModel(C#)  
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 화면에 보이는 상태면 true
            // 화면에 안 보이면 false
            if (value is Visibility vis)
            {
                return vis == Visibility.Visible;
            }

            return false;
        }
    }
}
