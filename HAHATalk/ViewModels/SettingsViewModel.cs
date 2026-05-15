using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HAHATalk.Messages;
using HAHATalk.Services;
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
        private readonly IFriendService _friendService;
        private readonly ApiSettings _apiSettings;

        // ObservableProperty : 자동으로 UserName, FriendCount프로퍼티 생성 
        [ObservableProperty]
        private string _userName = "";

        [ObservableProperty]
        private int _friendCount = 0;

        [ObservableProperty]
        private string _profileImage = ""; // 프로필 이미지 바인딩용

        // 창을 닫기 위한 Callback (WindowManager와 연동) 
        public Action? CloseAction { get; set; }

        // 생성자 주입 (UserStore 주입으로 현재 로그인된 유저 정보 가져오기) 
        public SettingsViewModel(UserStore userStore, IFriendService friendService, ApiSettings apiSettings)
        {
            _userStore = userStore;
            _friendService = friendService;
            _apiSettings = apiSettings;

            // UserStore에 저장된 현재 사용자 정보를 View에 바인딩할 변수에 할당 
            if (_userStore.CurrentUserId != null)
            {
                UserName = _userStore.CurrentUserNickname;

                // 🚀 내 프로필 이미지 로드
                LoadMyProfileImage();

                // 🚀 친구 수 비동기로 가져오기 (비동기 래퍼 호출)
                _ = LoadFriendCountAsync();

            }

            // 프로필 변경 메시지 구독 (설정창 띄워놓고 프로필 바꿨을 때 대비)
            WeakReferenceMessenger.Default.Register<MyProfileChangedMessage>(this, (r, m) =>
            {
                UserName = _userStore.CurrentUserNickname;
            });

            WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (r, m) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    CloseAction?.Invoke(); // WindowManager가 등록해준 닫기 액션 실행
                });
            });
        }

        /// <summary>
        /// 서버 주소와 조합하여 프로필 이미지 경로를 완성합니다.
        /// </summary>
        private void LoadMyProfileImage()
        {
            if (!string.IsNullOrEmpty(_userStore.CurrentUserProfile))
            {
                string baseUrl = _apiSettings.BaseUrl.TrimEnd('/');
                string path = _userStore.CurrentUserProfile;

                // 전체 URL 생성 및 캐시 방지용 틱 추가 ㅋ
                string fullPath = path.StartsWith("http") ? path : $"{baseUrl}{path}";
                ProfileImage = $"{fullPath}?v={DateTime.Now.Ticks}";
            }
            else
            {
                // 기본 이미지 설정
                ProfileImage = "pack://application:,,,/HAHATalk;component/Assets/default_user.png";
            }
        }

        /// <summary>
        /// 서버 API를 통해 현재 유저의 친구 수를 로드합니다.
        /// </summary>
        private async Task LoadFriendCountAsync()
        {
            try
            {
                var friends = await _friendService.GetFriendsAsync(_userStore.CurrentUserId);
                FriendCount = friends?.Count ?? 0; //
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"친구 수 로드 실패: {ex.Message}");
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
