using HAHATalk.ViewModels;
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

namespace HAHATalk.Controls
{
    /// <summary>
    /// LockScreenControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LockScreenControl : UserControl
    {
        public LockScreenControl()
        {
            InitializeComponent();

            // 포커스를 바로 비밀번호 박스에 줘서 편의성 높이기 
            Loaded += (s, e) => LockPasswordBox.Focus();

            // 비밀번호가 바뀔 때마다 체크하는 이벤트 추가 
            LockPasswordBox.PasswordChanged += LockPasswordBox_PasswordChanged;
        }

        private void LockPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pwBox && pwBox.Password.Length == 4)
            {
                // 현재 View의 DataContext(ViewModel)를 가져와서 커맨드 실행!
                if (this.DataContext is LockScreenViewModel vm)
                {
                    // ViewModel에 만든 UnlockCommand를 호출하면서 비번 전달
                    vm.UnlockCommand.Execute(pwBox.Password);

                    // 틀렸을 경우를 대비해 입력창 비우기 (선택사항)
                    pwBox.Clear();
                }
            }
        }
    }
}
