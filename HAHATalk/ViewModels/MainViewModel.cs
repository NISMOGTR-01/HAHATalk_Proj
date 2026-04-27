using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAHATalk.Stores;
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
            UserStore userStore)
        {
            _navigationStore = mainNavigationStore;
            _userStore = userStore; // XAML에서 UserStore.TotalUnreadCount에 접근 가능 

            // Store 이벤트 구독 
            _navigationStore.CurrentViewModelChanged += CurrentViewModelChanged;
            _navigationStore.SlideTypeChanged += SlideTypeChanged;

            // 앱 시작할때 초기화면 설정 (로그인) 
            NavigateToLogin();

            /*
            mainNavigationStore.CurrentViewModelChanged += CurrentViewModelChanged;
            mainNavigationStore.SlideTypeChanged += SlideTypeChanged;
            // 처음에 시작하는 메인 화면은 Login화면으로 할당 
            mainNavigationStore.CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(LoginControlViewModel))!;
            // 생성자를 로그인 
            //_currentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(LoginControlViewModel))!;
            */
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
            _navigationStore.SlideType = SlideType.RightToLeft;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(LoginControlViewModel))!;
        }


        [RelayCommand]
        public void NavigateToFriendList()
        {
            _navigationStore.SlideType = SlideType.LeftToRight;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(FriendListControlViewModel))!;
        }

        [RelayCommand]
        public void NavigateToChatList()
        {
            _navigationStore.SlideType = SlideType.RightToLeft;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(ChatListControlViewModel))!;
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
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(ChangePwdControlViewModel))!;
            //CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(ChangePwdControlViewModel))!;
        }

        [RelayCommand]
        public void ToSignup()
        {
            _navigationStore.SlideType = SlideType.RightToLeft;
            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(SignupControlViewModel))!;
            //CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(SignupControlViewModel))!;
        }

        // 2026.04.05 추가 
        [RelayCommand]
        public void Logout()
        {
            _navigationStore.SlideType = SlideType.LeftToRight;

            _navigationStore.CurrentViewModel = (INotifyPropertyChanged)App.Current.Services.GetService(typeof(LoginControlViewModel));
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
