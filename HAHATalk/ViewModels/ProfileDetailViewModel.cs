using CommonLib.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HAHATalk.Messages;
using HAHATalk.Services;
using HAHATalk.Stores;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class ProfileDetailViewModel 
    {
        private readonly INavigationService _navigationService;
        private readonly IAccountService _accountService;   // 2026.04.10 AccountService 변경 
        private readonly IWindowManager _windowManager;     // 2026.03.24 WindowManager 추가 
        private readonly UserStore _userStore;  // 2026.04.01 추가 

        // 프로필 기본 데이터 
        [ObservableProperty]
        private string _userName = default!;    // 사용자 이름 

        [ObservableProperty]
        private string _statusMessage = default!;   // 상태 메세지 

        [ObservableProperty]
        private string _targetEmail = default!;   // 상태 메세지 

        [ObservableProperty]
        private string _profileImage = default!;    // 프로필 이미지 

        [ObservableProperty] 
        private string _backgroundImage = default!;


        // 본인(접속자) 기본 프로필 데이터 
        [ObservableProperty]
        private bool _isMe; // 나인지 아닌지 
        
        [ObservableProperty] 
        private bool _isEditMode;      // 편집 모드 활성화 여부

        [ObservableProperty]
        private string _validationText = "";

        private Dictionary<string, bool> _validatingDict;
        private Dictionary<string, bool> ValidatingDict => _validatingDict ??= new Dictionary<string, bool>();

        // 생성자 주입 
        public ProfileDetailViewModel(
            INavigationService navigationService, 
            IAccountService accountService, 
            Friend friend, 
            bool isMe, 
            IWindowManager windowManager, 
            UserStore userStore)
        {
            _navigationService = navigationService;
            _accountService = accountService;
            _windowManager = windowManager; // 2026.03.24 추가
            _userStore = userStore;         // 2026.04.01 Add 
            _isMe = isMe;

            // Friend 모델의 속성에 맞춰서 맵핑 
            UserName = friend.FriendName;
            StatusMessage = friend.StatusMsg;
            TargetEmail = friend.TargetEmail;

            // 2026.04.28 계정의 프로필 이미지로 가져오도록 
           
            if (!string.IsNullOrEmpty(friend.ProfileImg))
            {
                // 리스트에서 잘 나오는 그 경로 형식을 그대로 넣어줍니다.
                this.ProfileImage = friend.ProfileImg;
            }
            BackgroundImage = "pack://application:,,,/Assets/default_background.png";
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            if(!IsMe)
            {
                return;
            }

            if(IsEditMode)
            {
                if (IsValidProfile())
                {
                    UdpateProfile();
                    IsEditMode = false;
                }
            }
            else
            {
                IsEditMode = true;
            }
        }

        // [1:1 채팅시작]
        [RelayCommand]
        private void StartChat(object obj)
        {
            // 현재 로그인한 사용자 정보 (
            string myEmail = _userStore.CurrentUserEmail ?? "";

            if (string.IsNullOrEmpty(myEmail))
            {
                MessageBox.Show("사용자 정보를 찾을 수 없습니다.");
                return;
            }

            // RoomId 생성 
            string roomId = CreateRoomId(myEmail, TargetEmail);

            // WindowManager로 채팅창 열기 (ID, 상대방 이름, 프로필 이미지 전달) 
            _windowManager.ShowChatRoom(roomId, TargetEmail, UserName, ProfileImage);


            // 현재 프로필 상세창 닫기 
            if(obj is Window currentWindow)
            {
                currentWindow.Close();
            }
        }

        // 고유 RoomId 생성 
        private string CreateRoomId(string email1,  string email2)
        {
            var list = new List<string> { email1, email2};
            list.Sort(); // 항상 일정한 순서로 정렬 (A_B 형태) 
            return string.Join("_", list);
        }

        // 창닫기 
        [RelayCommand]
        private void Close(object obj)
        {
            if(obj is Window window)
            {
                window.Close();
            }
        }

  
       

        // 비즈니스 로직 & 유효성 체크
        private bool IsValidProfile()
        {
            ClearValidating();

            if(string.IsNullOrWhiteSpace(UserName))
            {
                SetValidating("EmptyName");
                return false;
            }

            return true;
        }

        private void UdpateProfile()
        {
            try
            {
                // MSSQL 업데이트 호출 부분 
                // MSSQL 업데이트 호출 부분 (Zinn님의 Repository 패턴에 맞게 수정)
                // Account account = new Account { Name = UserName, StatusMessage = StatusMessage ... };
                // _accountRepository.MSSQL_Profile_Update(account);

                MessageBox.Show("프로필이 성공적으로 저장되었습니다.");
            }
            catch (Exception ex)
            {
                ValidationText = $"저장 중 오류발행: {ex.Message}";
            }
        }

        private void SetValidating(string key)
        {
            ValidatingDict[key] = true;
            switch(key)
            {
                case "EmptyName":
                    ValidationText = "이름은 필수 입력 사항입니다.";
                    break;
            }
        }

        private void ClearValidating()
        {
            ValidatingDict.Clear();
            ValidationText = "";
        }

        [RelayCommand]
        private async Task EditProfile()
        {
            // 1. 내 프로필인지 체크
            if (!IsMe) return;

            // 2. 프로필 편집 창 띄우기 (현재 값들을 인자로 전달)
            bool? result = _windowManager.ShowProfileEdit(UserName, StatusMessage, ProfileImage);

            // 3. [확인] 버튼을 눌러서 닫힌 경우 (서버 저장 성공 시)
            if (result == true)
            {
                // 서버에서 최신 정보 다시 가져오기 
                var updatedAccount = await _accountService.GetAccountAsnyc(_userStore.CurrentUserEmail);

                if (updatedAccount != null)
                {
                    // 상세창 UI 프로퍼티 업데이트 
                    UserName = updatedAccount.Nickname;
                    StatusMessage = updatedAccount.StatusMsg;

                    if (!string.IsNullOrEmpty(updatedAccount.ProfileImg))
                    {
                        string baseUrl = "https://localhost:7203";
                        string fullPath = updatedAccount.ProfileImg.StartsWith("http")
                            ? updatedAccount.ProfileImg
                            : $"{baseUrl}{updatedAccount.ProfileImg}";

                        // 캐시 방지 틱 추가하여 이미지 즉시 갱신
                        ProfileImage = $"{fullPath}?v={DateTime.Now.Ticks}";
                    }

                    // ✅ 오직 '확인'을 눌러 성공했을 때만 다른 화면(친구 목록 등)에 알림을 쏩니다.
                    WeakReferenceMessenger.Default.Send(new MyProfileChangedMessage());
                }
            }
            // 4. [취소] 버튼을 누르거나 창을 그냥 닫은 경우
            else
            {
                // 아무 작업도 하지 않습니다. 
                // 이렇게 하면 편집 창에서 설령 사진을 골랐더라도, '확인'을 누르지 않았다면
                // 상세 창과 친구 목록 창의 데이터는 그대로 유지됩니다. ㅋ
                System.Diagnostics.Debug.WriteLine("프로필 편집이 취소되었습니다.");
            }
        }
    }
}
