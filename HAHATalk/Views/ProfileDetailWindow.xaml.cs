using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HAHATalk.Views
{
    /// <summary>
    /// ProfileDetailWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProfileDetailWindow : Window
    {
        public ProfileDetailWindow()
        {
            InitializeComponent();

            // 2026.03.20
            // 창을 잡고 드래그할수 있도록 설정 
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            };
        }
    }
}
