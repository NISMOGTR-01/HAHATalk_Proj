using CommonLib.Models;
using CommonLib.Dtos;
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

        Task MarkAsReadAsync(string roomId, string userId);

        // 2026.05.07 파일 업로드 기능 추가 
        Task<string> UploadFileAsync(string localPath);

        //2026.05.09 Add
        string GetServerFullUrl(string relativeUrl);

        // 2026.05.11 Add 
        Task<FileUploadResponseDto> UploadFileExtendedAsync(string localFilePath);

    }
}
