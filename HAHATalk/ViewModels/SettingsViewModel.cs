using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAHATalk.Stores;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class SettingsViewModel
    {
        private readonly UserStore _userStore;

        // ObservableProperty : 자동으로 UserName, FriendCount프로퍼티 생성 
        [ObservableProperty]
        private string _userName = "";

        [ObservableProperty]
        private int _friendCount = 0;

        // 창을 닫기 위한 Callback (WindowManager와 연동) 
        public Action? CloseAction { get; set; }

        // 생성자 주입 (UserStore 주입으로 현재 로그인된 유저 정보 가져오기) 
        public SettingsViewModel(UserStore userStore)
        {
            _userStore = userStore;

            // UserStore에 저장된 현재 사용자 정보를 View에 바인딩할 변수에 할당 
            if(_userStore.CurrentUserId != null)
            {
                UserName = _userStore.CurrentUserNickname;
                //FriendCount = _userStore.

            }
        }

        [RelayCommand]
        private void EditProfile()
        {
            // 프로필 편집 로직 (나중에 API 연동)
            MessageBox.Show("프로필 편집 기능은 준비 중입니다.");
        }

        [RelayCommand]
        private void ManageFriends()
        {
            // 친구 관리 로직
            MessageBox.Show("친구 관리 화면으로 이동합니다.");
        }

        [RelayCommand]
        private void Close()
        {
            CloseAction?.Invoke();
        }
    }
}
