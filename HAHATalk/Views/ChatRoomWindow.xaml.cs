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
using System.Collections.Specialized;
using HAHATalk.Controls; // 2025.04.20 추가 
using System.Linq;

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

            // DataContext가 변경될 때마다 이벤트 등록 
            this.DataContextChanged += ChatRoomWindow_DataContextChanged;

            // 창이 닫힐 때 이벤트 해제 (메모리 누수방지) 
            this.Unloaded += (s, e) =>
            {
                if(this.DataContext is ChatRoomViewModel viewModel)
                {
                    viewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
                }

                // DataContextChanged 이벤트 자체도 해제하여 참조를 완전히 정리 
                this.DataContextChanged -= ChatRoomWindow_DataContextChanged;
            };

            // 2026.04.22 채팅창이 포커스를 얻었을 때 
            this.Activated += async (s, e) =>
            {
                if(this.DataContext is ChatRoomViewModel viewModel)
                {
                    // 1. 창이 활성화되면 바로 메시지 입력 가능하게 포커스 이동
                    MessageInput.Focus();

                    viewModel.IsWindowActive = true;

                    // 2. 읽음 처리 로직 (기존 유지)
                    var unreadFromPartner = viewModel.Messages
                        .Where(x => !x.IsMine && !x.IsRead)
                        .ToList();

                    if (unreadFromPartner.Any())
                    {
                        // 서버에 읽었다고 알림
                        await viewModel.MarkAllReadAsync();
                    }
                }
            };

            // 창이 focus를 잃은 경우 (다른 창 클릭, 최소화 등) 
            this.Deactivated += (s, e) =>
            {
                if(this.DataContext is ChatRoomViewModel viewModel)
                {
                    viewModel.IsWindowActive = false;
                }
            };
        }

        private void ChatRoomWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 이전 DataContext의 이벤트 해제 
            if(e.OldValue is ChatRoomViewModel oldVm)
            {
                oldVm.Messages.CollectionChanged -= Messages_CollectionChanged;
            }

            if (e.NewValue is ChatRoomViewModel vm)
            {
                vm.Messages.CollectionChanged += Messages_CollectionChanged;

                // 초기 데이터 로딩 시 스크롤 최하단 이동 
                ScrollToBottom();
            }                
        }

        // 메세지 추가 시 스크롤 이동 핸들러 
        private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                ScrollToBottom();
            }
        }

        private void ScrollToBottom()
        {
            // UI 스레드 안전성 및 레이아웃 업데이트 후 실행 보장 
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if(ChatScroller != null)
                {
                    ChatScroller.ScrollToEnd();
                }
  
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        // 채팅창 드래그 기능 (상단 헤더)  
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 왼쪽 버튼을 눌렸을 때만 실행 
            if(e.ChangedButton == MouseButton.Left)
            {
                // 마우스 왼쪽 버튼 상태가 실제로 'Pressed'인지 다시한번 확인 
                if(e.LeftButton == MouseButtonState.Pressed)
                {
                    try
                    {
                        this.DragMove();
                    }
                    catch(InvalidOperationException)
                    {

                    }
                }
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
                // Shift가 안 눌렸을 때 -> 전송 명령 실행 
                if(Keyboard.Modifiers == ModifierKeys.None)
                {
                    e.Handled = true; // 엔터가 텍스트박스에 입력되는 걸 막음 (줄바꿈방지) 

                    if (DataContext is ChatRoomViewModel viewModel)
                    {
                        if (viewModel.SendMessageCommand != null && viewModel.SendMessageCommand.CanExecute(null))
                        {
                            viewModel.SendMessageCommand.Execute(null);

                            // 메시지 전송 후 다시 포커스를 입력창으로 (계속 타이핑 가능하게)
                            MessageInput.Focus();
                        }
                    }
                }
            }
            
            // 3. 만약 Shift + Enter라면? -> e.Handled를 안 했으므로 자연스럽게 다음 줄로 넘어감 ㅋ

        }
    }
}
