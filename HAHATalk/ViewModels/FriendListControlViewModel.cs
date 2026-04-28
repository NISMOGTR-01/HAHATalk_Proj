using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAHATalk.Controls;
using CommonLib.Models;
using HAHATalk.Services;
using HAHATalk.Stores;
using HAHATalk.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using HAHATalk.Messages;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class FriendListControlViewModel
    {
        // 트리거가 감시할 이름표 
        public string ControlName => "FriendList";

        // 서비스 주입을 위한 필드 
        private readonly INavigationService _navigationService;
        private readonly IFriendService _friendService;   // 2026.03.19 FriendService 추가 
        private readonly IAccountService _accountService; // 2026.03.21 AccountService 추가 
        private readonly UserStore _userStore;

        private readonly IWindowManager _windowManager;      // 2026.03.24 windowManager 추가 

        // XAML의 ItemsControl ItemsSource={Binding Friends}
        [ObservableProperty]
        private ObservableCollection<Friend> _friends;

        // XAML의 TextBlock Text={Binding FriendsCountText}
        [ObservableProperty]
        private string _friendsCountText = "";

        [ObservableProperty]
        private Friend _myProfile = default!;

        // 2026.03.19 추가 (팝업 가시성 및 입력데이터) -> 친구 등록 
        [ObservableProperty]
        private bool _isAddFriendVisible;   // 팝업 /열기 닫기 (BoolToVis 컨버터 연결) 
        [ObservableProperty]
        private string _newFriendName = "";
        [ObservableProperty]
        private string _newFriendEmail = "";
        [ObservableProperty]
        private string _newFriendPhone = "";



        public FriendListControlViewModel(INavigationService navigationService, UserStore userStore, IFriendService friendService, 
                                            IAccountService accountService, IWindowManager windowManager)
        {
            this._navigationService = navigationService;
            this._userStore = userStore;
            this._friendService = friendService;
            this._accountService = accountService;
            this._windowManager = windowManager;
            // 초기 데이터 세팅
            Friends = new ObservableCollection<Friend>();

            // 내 카톡 프로필 초기화 (로그인 정보 기반) 
            InitializeMyProfile();

            WeakReferenceMessenger.Default.Register<MyProfileChangedMessage>(this, (r, m) =>
            {
                // 방송이 들리면 내 정보를 새로고침하는 메서드 호출 
                App.Current.Dispatcher.Invoke(async () => await RefreshMyInfo());
            });

            _ = InitializeAsync();
        }
        
        // 초기화 전용 비동기 래퍼 (2026.03.31) 
        private async Task InitializeAsync()
        {
            await LoadFriends();
        }

        private void InitializeMyProfile()
        {
            // 
            MyProfile = new Friend
            {
                FriendName = _userStore.CurrentUserNickname,
                TargetEmail = _userStore.CurrentUserId,
                StatusMsg = "오늘도 화이팅!",
                ProfileImg = _userStore.CurrentUserProfile
            };

            _ = RefreshMyInfo();
        }



        // 2026.03.31 비동기로 친구 목록 불러오도록 수정 
        // 친구 목록 불러오는 로직 (나중에 Repository를 주입받으면 DB 연동으로 바꿀 부분)
        private async Task LoadFriends()
        {
            // UserStore에서 로그인 시 저장해둔 ID를 가져온다. 
            string myId = _userStore.CurrentUserId;
            
            if (string.IsNullOrEmpty(myId))
            {
                FriendsCountText = "로그인 정보가 없습니다.";
                return;
            }

            try
            {
                // 비동기 방식으로 변경 (2026.03.31)
                // DB에서 친구 목록 가져오기 
                var dbFriends = await _friendService.GetFriendsAsync(myId);

                // UI 스레드에서 컬렉션 업데이트 
               
                Friends.Clear();
                if (dbFriends != null)
                {
                    foreach (var friend in dbFriends)
                    {
                        Friends.Add(friend);
                    }
                }
                // 상단 카운트 텍스트 업데이트 
                FriendsCountText = $"친구 {Friends.Count}명";
                
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"친구 목록 로드 실패 : {ex.Message}");
            }
           
        }

        // 프로필 상세 창 열기 (더블 클릭 이벤트 대응)
        [RelayCommand]
        private void OpenProfile(Friend selectedFriend)
        {
            if(selectedFriend == null)
            {
                return;
            }

            // 1.내프로필 or 친구 프로필 여부 판단 
            // UserSotre의 ID와 선택된 친구의 TargetEmail을 비교 
            bool isMe = selectedFriend.TargetEmail == _userStore.CurrentUserId;

            // 2.ProfileDetailViewModel 생성 및 의존성 주입 
            // 생성자 parameter 순서 
            // Navigation , repository, friend, isMe, windwowmanager, userStore
            var profileVm = new ProfileDetailViewModel
                (
                    _navigationService, 
                    _accountService,              
                    selectedFriend, 
                    isMe,
                    _windowManager, 
                    _userStore
                );

            // 3. Window 생성 및 데이터 context 연결 
            var profileWindow = new ProfileDetailWindow();
            profileWindow.DataContext = profileVm;

            // 4. 소유자 지정 및 실행 
            profileWindow.Owner = Application.Current.MainWindow;
            profileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Modal 
            profileWindow.Show();
        }

        // 2026.03.20 추가 
        [RelayCommand]
        //private void AddFriend() => IsAddFriendVisible = true; // 팝업 띄우기 
        private void AddFriend()
        {
            // 1. 전용 뷰모델 생성 (필요한 서비스 주입)
            var addFriendVm = new AddFriendViewModel(_friendService, _userStore);

            // 2. 윈도우 생성 및 데이터 컨텍스트 연결
            var win = new AddFriendWindow(); // 방금 만드신 Window 클래스
            win.DataContext = addFriendVm;
            win.Owner = Application.Current.MainWindow;

            // 3. 뷰모델에서 완료 시 창을 닫을 수 있게 연결
            addFriendVm.CloseAction = () => win.Close();

            // 4. 창 띄우기
            win.ShowDialog();

            // 5. 창이 닫힌 후 친구 목록 새로고침 (선택 사항)
            _ = LoadFriends();
        }

        [RelayCommand]
        private void CloseAddFriend()
        {
            ClearAddFriendFields();
            IsAddFriendVisible = false; // 팝업 닫기 
        }

        [RelayCommand]
        private async Task ConfirmAddFriend()
        {
            if (string.IsNullOrWhiteSpace(NewFriendName) || string.IsNullOrWhiteSpace(NewFriendEmail))
                return;

            // 중복 체크 먼저 수행!
            bool isAlreadyCheck = await _friendService.IsFriendAlreadyExistsAsync(_userStore.CurrentUserId, NewFriendEmail);

            if (isAlreadyCheck == true)
            {
                MessageBox.Show("이미 등록된 친구입니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // 중복이면 여기서 중단!
            }

            // DB 저장 (Repository 활용)
            var result = await _friendService.AddFriendAsync(_userStore.CurrentUserId, NewFriendEmail, NewFriendName,
                "");

            if(result == false)
            {
                MessageBox.Show("친구등록에 실패했습니다.");
                return;

            }

            // UI 즉시 반영 (임시 객체 생성) 
            var newFriend = new Friend
            {
                MyEmail = NewFriendEmail,   // 등록하려는 친구 이메일
                TargetEmail = _userStore.CurrentUserId, // 로그인 계정 정보 (나중에 목록을 가져와야) 
                FriendName = NewFriendName, // 닉네임 
                StatusMsg = ""

            };

            Friends.Add(newFriend);
            FriendsCountText = $"친구 {Friends.Count}명";

            // 필드 초기화 및 닫기 
            CloseAddFriend();
        }

        private void ClearAddFriendFields()
        {
            NewFriendName = "";
            NewFriendEmail = "";
            NewFriendPhone = "";
            
        }

        [RelayCommand]
        private void NavigateToChatList()
        {
            // _navigationService를 통해 채팅 목록 화면으로 이동 
            _navigationService.Navigate(NaviType.ChatList);
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            // 나중에 설정 화면이 생기면 이동하는 로직
            // _navigationService.Navigate(NaviType.Settings);

            int a = 10;
        }

        // 친구 갱신 (비동기 방식으로 수정) 
        [RelayCommand]
        private async Task RefreshFriends()
        {
            await LoadFriends();
        }

        // 2026.03.19 친구 검색, 친구 등록 추가 
        [RelayCommand]
        private void Search()
        {

        }

        private async Task RefreshMyInfo()
        {
            // 서버에서 내 최신 정보를 다시 가져와서 
            // 친구 목록 상단에 있는 '와타시'의 프로퍼티들을 갱신 
            var me = await _accountService.GetAccountAsnyc(_userStore.CurrentUserEmail);

            if(me != null)
            {
                // MyProfile 의 속성 갱신 
                MyProfile.FriendName = me.Nickname;
                MyProfile.StatusMsg = me.StatusMsg;

                // 이미지 경로 처리 (BaseUrl + 캐시 방지 틱 추가) 
                if(!string.IsNullOrEmpty(me.ProfileImg))
                {
                    string baseUrl = "https://localhost:7203";
                    string fullPath = me.ProfileImg.StartsWith("https")
                        ? me.ProfileImg
                        : $"{baseUrl}{me.ProfileImg}";

                    // 틱값을 붙여줘야 목록의 이미지도 '새로고침'
                    MyProfile.ProfileImg = $"{fullPath}?v={DateTime.Now.Ticks}";

                }

                // 만약 MyProfile 자체가 바뀐걸 UI가 모르는 경우 강제로 신호를 전달
                // // 
            }
        }
    }
}
