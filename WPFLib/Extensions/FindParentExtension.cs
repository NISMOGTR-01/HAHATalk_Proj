using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace WPFLib.Extensions
{
    public static class FindParentExtension
    {
        // 재귀함수 
        public static T? Findparent<T>(this DependencyObject child)
            where T : DependencyObject
        {
            return FindParent<T>(child, null);
        }

        // 재귀함수 
        public static T? FindParent<T>(this DependencyObject child, string? parentName)
            where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            // 부모가 비어 있는 경우 
            if(parent == null)
            {
                return null;
            }
        
            // 형변환 
            var frameworkElement = (FrameworkElement)parent;

            // 부모이름이 null 
            // 프레임워크의 이름 == 부모이름 && 프레임워크 타입이 T) 
            if ((parentName == null || frameworkElement.Name == parentName) && frameworkElement is T)
            {
                return (T)parent;
            }
            else
            {
                return FindParent<T>(parent, parentName);
            }

        }
    }
}
