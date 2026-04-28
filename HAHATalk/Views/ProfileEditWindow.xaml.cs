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
    /// UserControl1.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProfileEditWindow : Window
    {
        public ProfileEditWindow()
        {
            InitializeComponent();

            // 드래그 이동 이벤트 활성화 
            this.MouseLeftButtonDown += ProfileEditWindow_MouseLeftButtonDown;
        }

        private void ProfileEditWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 마우스 왼쪽 버튼을 누른 상태에서 드래그 
            if(e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
