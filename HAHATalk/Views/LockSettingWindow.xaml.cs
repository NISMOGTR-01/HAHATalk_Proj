using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
    /// LockSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LockSettingWindow : Window
    {
        public LockSettingWindow()
        {
            InitializeComponent();

            // 화면이 켜지자 마자 1번째 암호 입력창에 focus 타겟팅 
            this.Activated += (s, e) => PasswordInput.Focus();
        }

        /// <summary>
        /// 2026.05.16 Add
        /// 입력창에 숫자만 입력되도록 제한하는 핸들러
        /// </summary>
        private void Numeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 숫자가 아닌 문자가 포함되어 있다면 입력을 차단(Handled = true)합니다.
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // 상단 드래그 이동 핸들러
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                }
                catch (InvalidOperationException) { }
            }
        }

        // 우측 상단 X 버튼 누를 시 닫기
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
