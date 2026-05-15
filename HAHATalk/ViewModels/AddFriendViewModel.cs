using CommonLib.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HAHATalk.Messages;
using HAHATalk.Services;
using HAHATalk.Stores;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class AddFriendViewModel
    {
        private readonly IFriendService _friendService;
        private readonly UserStore _userStore;

        [ObservableProperty]
        private string _newFriendName = "";

        [ObservableProperty]
        private string _newFriendEmail = "";

        [ObservableProperty]
        private string _newFriendPhone = "";

        // 창을 닫기 위한 Callback 
        public Action? CloseAction
        {
            get; set;
        }

        // 생성자 
        public AddFriendViewModel(IFriendService friendService, UserStore userStore)
        {
            this._friendService = friendService;
            this._userStore = userStore;

            // 2026.05.15 로그아웃 메세지 등록 
            WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (r, m) =>
            {
                // UI 스레드에서 안전하게 창 닫기 실행 
                App.Current.Dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Debug.Write("로그아웃 감지 : 친구 추가 창을 닫습니다.");
                    CloseAction?.Invoke();
                });
            });
        }

        [RelayCommand]
        private async Task ConfirmAddFriend()
        {
            if(string.IsNullOrWhiteSpace(NewFriendName) || 
                string.IsNullOrWhiteSpace(NewFriendEmail))
            {
                MessageBox.Show("이름과 이메일을 입력해주세요.");
                return;
            }

            try
            {
                // 1. 중복 체크
                bool exists = await _friendService.IsFriendAlreadyExistsAsync(_userStore.CurrentUserId, NewFriendEmail);
                if (exists)
                {
                    MessageBox.Show("이미 등록된 친구입니다.");
                    return;
                }

                // 서버 저장 
                var result = await _friendService.AddFriendAsync(_userStore.CurrentUserId, NewFriendEmail, NewFriendName, "");
            
                if(result)
                {
                    // [핵심] 성공 시 채팅 목록 갱신 메시지 전송
                    WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());

                    MessageBox.Show($"{NewFriendName}님이 친구로 등록되었습니다.");

                    // 창 닫기
                    CloseAction?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}");
            }
        }
    }
}
