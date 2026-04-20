
using HAHATalk.Stores;
using HAHATalk.ViewModels;
using HAHATalk.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace HAHATalk.Services
{
    public class WindowManager : IWindowManager
    {
        // 현재 열려있는 모든 채팅방을 관리 (중복 오픈 방지) 
        private readonly Dictionary<string, ChatRoomWindow> _chatWindows = new();
        
        private readonly IChatService _chatService; // 2026.04.10 ChatService 추가 
        private readonly UserStore _userStore;
        private readonly ISignalRService _signalRService;

        public WindowManager(IChatService chatService, UserStore userStore,
            ISignalRService signalRService)
        {
            _chatService = chatService;
            _userStore = userStore;
            _signalRService = signalRService;
        }


        public void ShowChatRoom(string roomId, string targetId, 
            string targetName, string targetProfile = "")
        {
            // 2026.04.01 키를 roomId로 변경 
            // 이미 해당 친구와의 채팅창이 열려 있는 경우 
            if (_chatWindows.ContainsKey(roomId))
            {
                var existingWindow = _chatWindows[roomId];
                
                // window가 살아 있다면 활성화 
                if (existingWindow.IsLoaded)
                {
                    existingWindow.Activate();  // 맨 앞으로 가져옴 
                    return;
                }
                else
                {
                    _chatWindows.Remove(roomId); // 닫혔다면 리스트에서 제거 
                }
            }

            // View는 기본생성자로 생성 
            var chatRoomWindow = new ChatRoomWindow();

            // 2026.04.01 생성자 파라미터 추가 
            // ViewModel은 생성 시에 이름을 넣어줌 
            var chatRoomViewModel = new ChatRoomViewModel(
                roomId, 
                targetId,
                targetName, 
                _chatService, 
                _userStore,
                _signalRService,
                targetProfile);

            // view, viewModel 결합 
            chatRoomWindow.DataContext = chatRoomViewModel;
            
            // 윈도우 닫힐때 딕셔너리에서 제거 
            chatRoomWindow.Closed += (s, e) => _chatWindows.Remove(roomId);
            
            _chatWindows[roomId] = chatRoomWindow;
            
            chatRoomWindow.Show();
        }

        public void CloseAll()
        {
            throw new NotImplementedException();
        }

    }
}
