using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommonLib.Models;
using CommunityToolkit.Mvvm.Messaging;
using HAHATalk.Services;
using HAHATalk.Stores;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.Text;
using HAHATalk.Messages;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class ChatListControlViewModel
    {
        // 트리거가 감시할 이름표 
        public string ControlName => "ChatList";

        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;  // 2026.04.06 접속 정보 Store 
        private readonly IChatService _chatService; // 2026.04.06 ChatRepository 
        private readonly IWindowManager _windowManager; // 2026.04.06 WIndowManager


        // 채팅방 목록을 담는 콜렉션 
        [ObservableProperty]
        private ObservableCollection<ChatList> _chatList = new();

        // 2026.04.06 채팅 리스트 (선택된) 
        [ObservableProperty]
        private ChatList? _selectedChat;

        public ChatListControlViewModel(INavigationService navigationService, 
                UserStore userStore, 
                IChatService chatService, 
                IWindowManager windowManager)
        {
            _navigationService = navigationService;
            _userStore = userStore;
            _chatService = chatService;
            _windowManager = windowManager;

            // 2024.04.14 메신저 등록 
            // ChatRoom에서 새 메세지를 보냈을때 (처음 채팅 포함) 이 신호를 받아 목록을 새로 고침 
            WeakReferenceMessenger.Default.Register<RefreshChatListMessage>(this, (r, m) =>
            {
                // UI 스레드에서 안전하게 로드 실행 
                App.Current.Dispatcher.Invoke(async () =>
                {
                    await LoadChatListAsync();
                });
            });

            // 우선 테스트용 더미 데이터를 제작 
            //LoadDummyData();
            _ = LoadChatListAsync();
        }

      
        // 
        private async Task LoadChatListAsync()
        {
            // UserStore에서 현재 로그인한 사용자의 이메일 가져오기 
            string currentUserEmail = _userStore.CurrentUserEmail;

            // 
            if(string.IsNullOrEmpty(currentUserEmail))
            {
                return;
            }

            // ChatRepository의 MSSQL_GetChatListAsync 메서드 호출 
            var rooms = await _chatService.GetChatListAsync(currentUserEmail);

            // UI 콜렉션 업데이트 
            ChatList.Clear();

            if(rooms != null)
            {
                foreach(var room in rooms)
                {
                    ChatList.Add(room);
                }
            }

            // 전체 안 읽은 메세지 수도 Repository를 통해 업데이트 
            _userStore.TotalUnreadCount = await _chatService.GetTotalUnreadCountAsync(currentUserEmail);
        }

        // 채팅방 클릭할 경우 해당 방으로 이동하는 RelayCommand 
        [RelayCommand]
        public void OpenChatRoom(ChatList? selectedList)
        {
            // 2026.04.06 
            // parameter가 null이면 binging SelectedChat에서 가져오기 
            var targetChat = selectedList ?? SelectedChat;

            if (targetChat == null)
                return;

            // 이미 리스트에서 (selectedList) DB에서 가져온 RoomI 및 관련 정보 존재 
            _windowManager.ShowChatRoom(
                targetChat.RoomId,
                targetChat.TargetId,
                targetChat.TargetName,
                targetChat.ProfileImg);



            // chatRoom으로 이동하는 함수 
            //_navigationService.Navigate(NaviType.ChatRoom);
        }

       

    }
}
