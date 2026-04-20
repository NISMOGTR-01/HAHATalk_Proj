using System;
using System.Collections.Generic;
using System.Text;

namespace HAHATalk.Services
{
    public enum NaviType
    {
        None, 
        Login,                  // 로그인 
        ChangePwd,              // 비밀번호 변경 
        Signup,                 // 회원가입 
        FindAccount,            // 계정찾기 
        FriendList,             // 2026.03.17 FriendList 화면 추가 
        ChatList,               // 채팅방 목록
        ChatRoom                // 채팅방 (1:1 or 단톡방) 
    }
}
