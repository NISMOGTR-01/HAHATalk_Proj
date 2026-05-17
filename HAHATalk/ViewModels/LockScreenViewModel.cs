using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAHATalk.Services;
using HAHATalk.Stores;
using System.Windows;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class LockScreenViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;
        private readonly ApiSettings _apiSettings;

        [ObservableProperty]
        private string _userEmail;

        [ObservableProperty]
        private string _profileImage;

        // 잠금 해제 성공시 실행할 액션 (View에서 Close() 등을 호출할 때 사용) 
        public Action? OnUnlockSuccess { get; set; }

        public LockScreenViewModel(INavigationService navigationService,
            UserStore userStore,
            ApiSettings apiSettings)
        {
            _navigationService = navigationService;
            _userStore = userStore;
            _apiSettings = apiSettings;

            // UserStore에서 현재 사용자 정보 불러오기 
            UserEmail = _userStore.CurrentUserEmail;

            // 프로필 이미지 경로 처리 
            if (!string.IsNullOrEmpty(_userStore.CurrentUserProfile))
            {
                string baseUrl = _apiSettings.BaseUrl.TrimEnd('/');
                string profilePath = _userStore.CurrentUserProfile;

                // 이미 전체 경로(http)인 경우를 제외하고 결합 
                string fullPath = profilePath.StartsWith("http")
                               ? profilePath
                               : $"{baseUrl}{profilePath}";

                // 캐시 방지를 위해 틱값을 붙여주기ㅋ
                ProfileImage = $"{fullPath}?v={DateTime.Now.Ticks}";
            }
            else
            {
                // 프로필이 없을 때 보여줄 기본 이미지 (프로젝트에 Assets 폴더와 이미지가 있어야 함)
                ProfileImage = "pack://application:,,,/HAHATalk;component/Assets/default_profile.png";
            }  
        }

        [RelayCommand]
        private void Unlock(string password)
        {
            string savedPassword = _userStore.LockPassword ?? "0000";

            if (password == savedPassword)
            {
                // 성공 시 이전 화면으로 돌아가거나 메인으로 이동
                _navigationService.Navigate(NaviType.FriendList);

                OnUnlockSuccess?.Invoke();
            }
            else
            {
                //
                MessageBox.Show("비밀번호가 일치하지 않습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        [RelayCommand]
        private void CloseApp()
        {
            Application.Current.Shutdown();
        }

    }
}
