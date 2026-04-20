using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace WPFLib.Converters
{
    // 정수 값이 0보다 크면 visible, 0이하면 Collapsed를 반환하는 컨버터 
    // 채팅 배지, 알림 개수 등 숫자에 따른 UI 노출 여부 결정시 사용 
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. 데이터가 들어왔는지 확인하고 정수형으로 변환 시도
            if (value != null && int.TryParse(value.ToString(), out int count))
            {
                // 2. 0보다 크면 보이고, 0 이하면 공간까지 숨김
                return (count > 0) ? Visibility.Visible : Visibility.Collapsed;
            }

            // 3. 데이터가 비정상적일 경우 기본적으로 숨김 처리
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // UI 상태를 숫자로 바꿀 일은 거의 없으므로 구현하지 않거나 Binding.DoNothing 반환
            return Binding.DoNothing;
        }
    }
}
