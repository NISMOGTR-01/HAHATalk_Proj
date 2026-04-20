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

namespace HAHATalk.Views
{
    /// <summary>
    /// ChatRoomWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChatRoomWindow : Window
    {
        public ChatRoomWindow()
        {
            InitializeComponent();

        }

        // 채팅창 드래그 기능 추가 
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                this.DragMove(); 
            }
        }


        // 닫기 버튼 로직 
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            // 엔터키를 눌렀을 때만 동작 
            if (e.Key == Key.Enter)
            {
                // Shift가 안 눌렸을 때 -> 전송 
                if(Keyboard.Modifiers == ModifierKeys.None)
                {
                    e.Handled = true; // 엔터가 텍스트박스에 입력되는 걸 막음 (줄바꿈방지) 

                    var viewModel = this.DataContext as ChatRoomViewModel;
                    if (viewModel != null && viewModel.SendMessageCommand.CanExecute(null))
                    {
                        viewModel.SendMessageCommand.Execute(null);
                    }
                }
            }
            
            // 3. 만약 Shift + Enter라면? -> e.Handled를 안 했으므로 자연스럽게 다음 줄로 넘어감 ㅋ

        }
    }
}
