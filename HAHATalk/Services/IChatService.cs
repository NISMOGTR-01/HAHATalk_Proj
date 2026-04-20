using CommonLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HAHATalk.Services
{
    public interface IChatService
    {
        Task<List<ChatList>> GetChatListAsync(string email);

        Task<List<ChatMessage>> GetChatHistoryAsync(string roomId);

        Task<int> GetTotalUnreadCountAsync(string email);

        Task<bool> SaveMessageAsync(ChatMessage message);

        // 채팅 목록 업데이트 
        Task<bool> UpdateChatListAsync(ChatMessage message, string targetId,
            string targetName, string myId, string myNickname);
    }
}
