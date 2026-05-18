using CommonLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HAHATalk.Services
{
    public interface IFriendService
    {
        // 친구 목록 불러오기 
        Task<List<Friend>> GetFriendsAsync(string myId);

        // 친구 추가 
        Task<bool> AddFriendAsync(string myId, string friendEmail,
            string friendName, string statusMsg);

        // 친구 중복 확인 
        Task<bool> IsFriendAlreadyExistsAsync(string myId, string friendEmail);

        // 친구 삭제 기능 
        Task<bool> DeleteFriendAsync(string myId, string friendEmail);
    }
}
