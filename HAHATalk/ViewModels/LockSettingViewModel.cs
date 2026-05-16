using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAHATalk.Services;
using HAHATalk.Stores;
using System;
using System.Windows;


namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class LockSettingViewModel
    {
        private readonly UserStore _userStore;
        private readonly IAccountService _accountService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SetPasswordCommand))]
        private string _password = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SetPasswordCommand))]
        private string _confirmPassword = string.Empty;

        /// <summary>
        /// 2026.05.16 Add
        /// SettingsViewModel과 동일하게 창을 닫기 위한 Action 선언
        /// </summary>
        public Action? CloseAction { get; set; }

        public LockSettingViewModel(UserStore userStore, 
            IAccountService accountService)
        {
            _userStore = userStore;
            _accountService = accountService;
        }

        /// <summary>
        /// 2026.05.16 Add
        /// 보안을 위해 입력 필드와 메모리를 강제로 공백 초기화하는 메서드
        /// </summary>
        public void ClearFields()
        {
            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }

        /// <summary>
        /// 확인 버튼 활성화 조건 (4자리 숫자가 모두 채워지고 두 값이 일치할 때 true)
        /// </summary>
        private bool CanSetPassword()
        {
            return !string.IsNullOrEmpty(Password) && Password.Length == 4 &&
                   !string.IsNullOrEmpty(ConfirmPassword) && ConfirmPassword.Length == 4 &&
                   Password == ConfirmPassword;
        }

        /// <summary>
        /// 암호 설정 실행 명령
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSetPassword))]
        private async Task SetPassword()
        {
            try
            {
                string currentEmail = _userStore.CurrentUserEmail;

                if (string.IsNullOrEmpty(currentEmail))
                {
                    MessageBox.Show("사용자 세션 정보가 올바르지 않습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 1. API 서버로 잠금 암호 변경 요청 전송
                bool isSuccess = await _accountService.UpdateLockPasswordAsync(currentEmail, Password);

                if (isSuccess)
                {
                    // 2. 서버 DB 저장이 완벽히 확인되면 메모리 캐시(UserStore) 상태 갱신
                    _userStore.LockPassword = Password;

                    MessageBox.Show("잠금모드 암호가 성공적으로 설정되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 성공시 메모리 데이터 클리어 후 창 닫기 
                    ClearFields();
                    CloseAction?.Invoke();
                }
                else
                {
                    MessageBox.Show("서버에 암호를 저장하지 못했습니다. 네트워킹 상태를 확인해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"암호 설정 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}