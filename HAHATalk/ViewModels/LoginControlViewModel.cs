using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using HAHATalk.Services;
using HAHATalk.Stores;
using CommonLib.Models;
using System.Collections.ObjectModel;
using CommonLib.Dtos;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class LoginControlViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IAccountService _accountService;
        private readonly IChatService _chatService;
        private readonly UserStore _userStore;
        private readonly ISignalRService _signalRService;       // 2026. 03.27 Signal R service 추가 

        [ObservableProperty]
        private ObservableCollection<string>? _emails;

        [ObservableProperty]
        private string _selectedEmail = ""; // 화면에서 선택하거나 입력한 이메일 

        [ObservableProperty]
        private string _password = "";  // 입력받을 패스워드

        [ObservableProperty]
        private string _validationText = "";    // 에러 메세지 표시용 

        // 2026.03.17 프로퍼티 추가 
        [ObservableProperty]
        private bool _isLoggingIn;

        // 생성자에서 필요한 서비스를 주입받는다. (의존성 주입) 
        public LoginControlViewModel(INavigationService navigationService, IAccountService accountService, IChatService chatService, 
            UserStore userStore, 
            ISignalRService signalRService)
        {
            this._navigationService = navigationService;
            this._accountService = accountService;
            this._chatService = chatService;
            this._userStore = userStore;
            this._signalRService = signalRService;

            Emails = new ObservableCollection<string>()
            {
                "test1@test.com",
                "test2@test.com",
                "test3@test.com",
            };

            SelectedEmail = Emails.FirstOrDefault()!;

        }

        [RelayCommand]
        //private void Login(object obj)
        private async Task Login(object obj)        // 2026.03.17 버튼 애니메이션 효과를 위해 비동기 형식으로 변경 
        {
            // 넘어온 프로젝트가 PasswordBoxControl인지 확인하고 패스워드 추출 
            if(obj is WPFLib.Controls.PasswordBoxControl pwControl)
            {
                this.Password = pwControl.Password;
            }

            // 입력 유효성 검사
            if (string.IsNullOrEmpty(SelectedEmail) || string.IsNullOrEmpty(Password))
            {
                ValidationText = "이메일과 비밀번호를 모두 입력해주세요.";
                return;
            }

            try
            {
                // 로그인 프로세스 시작
                IsLoggingIn = true;
                ValidationText = "Server와 통신 중입니다...";

                // 서비스 호출(CommonLib의 Account 모델을 실어보냄)
                var loginReq = new LoginRequestDto
                {
                    Email = SelectedEmail,
                    Pwd = Password
                };

                var response = await _accountService.LoginAsync(loginReq);
                
                // 계정이 있으면 
                if (response != null && response.IsSuccess)
                {
                    var account = response.UserAccount;

                    ValidationText = $"{account.Nickname}님 환영합니다!";

                    // UserStore  정보 저장(이미 서버에서 받아온 모델에 정보가 있음)
                    _userStore.CurrentUserEmail = account.Email;
                    _userStore.CurrentUserId = account.Email;
                    _userStore.CurrentUserNickname = account.Nickname;

                    // 프로필 이미지, 대화명 추가 
                    _userStore.CurrentUserProfile = account.ProfileImg;
                    _userStore.CurrentUserStatusMsg = account.StatusMsg;

                    // 나머지 후속 작업(Signal R  및 로컬 데이터 동기화)
                    ValidationText = "실시간 통신 연결 중입니다...";

                    // 읽지 않는 메세지 합산 
                    int totalUnread = await _chatService.GetTotalUnreadCountAsync(SelectedEmail);
                    _userStore.TotalUnreadCount = totalUnread;


                    await _signalRService.ConnectAsync();

                    await Task.Delay(500);
                    _navigationService.Navigate(NaviType.FriendList);
                }
                else
                {
                    ValidationText = response?.Message ?? "이메일 또는 비밀번호가 올바르지 않습니다.";
                    IsLoggingIn = false;
                }
              
            }
            catch(Exception ex)
            {
                // 디버깅을 위해 로그를 찍어두는 게 좋다.
                System.Diagnostics.Debug.WriteLine($"Login Error: {ex.Message}");
                ValidationText = "서버 연결 실패!!! (포트확인)";
                IsLoggingIn = false;
            }
           
        }
       
    }
}
