using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HAHATalk.Messages;
using HAHATalk.Services;
using HAHATalk.Stores;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using WPFLib.Controls;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class MainViewModel
    {
        //2026.03.18 navigationStore 변수로 추가 
        private readonly MainNavigationStore _navigationStore;
        // 2026.04.29 서비스 제공자 필드추가 
        private readonly IServiceProvider _serviceProvider; 
        // 2026.05.11 
        private readonly IChatService _chatService; // 추가 주입 (전역 안 읽은 메세지 관리용) 
        // 2026.05.13 
        private readonly ISignalRService _signalRService;

        [ObservableProperty]
        private INotifyPropertyChanged _currentViewModel = default!;

        [ObservableProperty]
        private SlideType _slideType = default!;

        // 2026.03.18 Add
        // 사이드바가 보일지 결정하는 속성 
        [ObservableProperty]
        private bool _isSideBarVisible;

        // 2026.04.22 UserStore 추가 
        [ObservableProperty]
        private UserStore _userStore;

        public MainViewModel(MainNavigationStore mainNavigationStore, 
            UserStore userStore,
            IServiceProvider serviceProvider, 
            IChatService chatService,
            ISignalRService signalRService)
        {
            _navigationStore = mainNavigationStore;
            _userStore = userStore; // XAML에서 UserStore.TotalUnreadCount에 접근 가능 
            _serviceProvider = serviceProvider;
            _chatService = chatService;
            _signalRService = signalRService;

            // Store 이벤트 구독 
            _navigationStore.CurrentViewModelChanged += CurrentViewModelChanged;
            _navigationStore.SlideTypeChanged += SlideTypeChanged;

          
            // MainViewModel.cs 생성자에 추가
            // 어떤 화면에 있더라도 새 메세지가 오면 count 갱신 
            WeakReferenceMessenger.Default.Register<NewMessageReceivedMessage>(this, (r, m) =>
            {
                // 어떤 화면에 있든 상관없이 메시지가 오면 카운트 갱신
                App.Current.Dispatcher.Invoke(async () =>
                {
                    await UpdateTotalUnreadCount();
                });
            });

            // 2026.05.12 상대방이 메시지를 읽어 카운트가 줄어들었을 때도 반영
            WeakReferenceMessenger.Default.Register<MessagesReadMessage>(this, (r, m) =>
            {
                App.Current.Dispatcher.Invoke(async () =>
                {
                    await UpdateTotalUnreadCount();
                });
            });

            // 앱 시작할때 초기화면 설정 (로그인) 
            NavigateToLogin();

        }

        /// <summary>
        /// DB에서 전체 안 읽은 메시지 수를 가져와 UserStore를 갱신합니다.
        /// </summary>
        private async Task UpdateTotalUnreadCount()
        {
            if (!string.IsNullOrEmpty(_userStore.CurrentUserEmail))
            {
                // ChatService를 MainViewModel에도 주입받아야 합니다.
                _userStore.TotalUnreadCount = await _chatService.GetTotalUnreadCountAsync(_userStore.CurrentUserEmail);
            }
        }

        private void CurrentViewModelChanged(INotifyPropertyChanged viewModel)
        {
            CurrentViewModel = viewModel;

            // 사이드바 표시 여부 결정 로직 
            if(viewModel is LoginControlViewModel ||
                viewModel is SignupControlViewModel || 
                viewModel is FindAccountControlViewModel || 
                viewModel is ChangePwdControlViewModel)
            {
                IsSideBarVisible = false;
            }
            else
            {
                IsSideBarVisible = true;
                // 사이드바가 나타나는 시점(로그인 성공 후 메인 진입 시)에 카운트 초기 로드
                _ = UpdateTotalUnreadCount();
            }
        }

        private void SlideTypeChanged(SlideType slideType)
        {
            SlideType = slideType;
        }

        // 사이드바 전용 커맨드 

        // 초기 시작용 
        private void NavigateToLogin()
        {
            //

            _navigationStore.SlideType = SlideType.RightToLeft;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)_serviceProvider.GetRequiredService<LoginControlViewModel>(); 
        }


        [RelayCommand]
        public void NavigateToFriendList()
        {
            _navigationStore.SlideType = SlideType.LeftToRight;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)_serviceProvider.GetRequiredService<FriendListControlViewModel>();
        }

        [RelayCommand]
        public void NavigateToChatList()
        {
            // 기존의 단순한 코드를 아래처럼 보완하는 겁니다.
            _navigationStore.SlideType = SlideType.RightToLeft;

            // 1. 서비스 제공자로부터 뷰모델을 명확히 가져오고
            var vm = _serviceProvider.GetRequiredService<ChatListControlViewModel>();

            // 2. 현재 화면을 교체한 뒤
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)vm;

            // 3. 채팅 리스트 뷰모델에게 "야, 화면 떴으니까 데이터 새로 불러와!"라고 신호를 쏴주는 겁니다.
            WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());
        }


        [RelayCommand]
        public void ToLogin()
        {
            NavigateToLogin();
            //CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(LoginControlViewModel))!;
        }

        [RelayCommand]
        public void ToChangePwd()
        {
            _navigationStore.SlideType = SlideType.RightToLeft;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)_serviceProvider.GetRequiredService<ChangePwdControlViewModel>();
            //CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(ChangePwdControlViewModel))!;
        }

        [RelayCommand]
        public void ToSignup()
        {
            _navigationStore.SlideType = SlideType.RightToLeft;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)_serviceProvider.GetRequiredService<SignupControlViewModel>();
            //CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(SignupControlViewModel))!;
        }

        // 2026.04.05 추가 
        [RelayCommand]
        public async Task Logout()
        {
            /// 1. SignalR 연결 해제
            await _signalRService.DisconnectAsync();

            // 2. UserStore 공통 데이터 초기화 
            _userStore.ClearSession();

            // 각 ViewModel에게 초기화 신호 보내기 
            WeakReferenceMessenger.Default.Send(new LogoutMessage()); 
            
            // 3. 로그인 화면으로 이동 (NavigationStore 활용)
            _navigationStore.SlideType = SlideType.LeftToRight;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)_serviceProvider.GetRequiredService<LoginControlViewModel>();
        }

        [RelayCommand]
        public void Exit()
        {
            // 어플리케이션 완전 종료 
            System.Windows.Application.Current.Shutdown();
        }

        [RelayCommand]
        public void OpenSettings()
        {
            
        }

        
    }
}
