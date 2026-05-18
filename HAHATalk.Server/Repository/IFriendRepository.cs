using CommonLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HAHATalk.Server.Repository
{
    public interface IFriendRepository
    {
        // 로그인 한 계정의 ID를 매개변수로 받아 친구목록(LIST)를 반환
        Task<List<Friend>> MSSQL_GetFriendsAsync(string myId);

        // 친구 추가 등록
        Task<bool> AddFriendAsync(string myId, string friendEmail, string friendName, string statusMsg); 

        // 친구 중복 확인 
        Task<bool> IsFriendAlreadyExistsAsync(string myId, string friendEmail);

        // [2026.05.18 추가] 친구 삭제 (DB 물리 삭제)
        Task<bool> MSSQL_DeleteFriendAsync(string myId, string friendEmail);
    }
}
