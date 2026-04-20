using HAHATalk.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace HAHATalk.Services
{
    public class UserService : IUserService
    {
        private readonly UserStore _userStore;

        // 생성자에서 UserStore를 주입받아 연결 
        public UserService(UserStore userStore)
        {
            _userStore = userStore;
        }

        // DB ID (친구 목록 조회 시 필수) 
        public string Id => _userStore.CurrentUserId;

        // 이메일 (채팅방 RoomId 생성시 필수) 
        public string Email => _userStore.CurrentUserEmail;

        // 닉네임 (상단 프로필 표시용)
        public string Name => _userStore.CurrentUserNickname;

        // 읽지 않은 메세지 총합 
        public int TotalUnreadCount => _userStore.TotalUnreadCount;

        // 로그인 체크 여부 (Email이 비어 있지 않으면 로그인으로 간주) 
        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(_userStore.CurrentUserEmail);
    }
}
