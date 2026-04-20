using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommonLib.Models;

namespace HAHATalk.Stores
{
    [ObservableObject]
    public partial class UserStore
    {
        // 친구 목록 조회할 때 필수인 (DB id컬럼 값)
        [ObservableProperty]
        private string _currentUserId = string.Empty;

        // 이메일 
        [ObservableProperty]
        private string _currentUserEmail = string.Empty;

        // 화면 상단에 사용할 닉네임 
        [ObservableProperty]
        private string _currentUserNickname = string.Empty;

        // 아직 읽지 않은 채팅 수 총합 
        [ObservableProperty]
        private int _totalUnreadCount;
    }
}
