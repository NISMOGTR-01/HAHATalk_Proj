using System;
using System.Collections.Generic;
using System.Text;
using CommonLib.Models;
using System.Threading.Tasks;
using CommonLib.Dtos;

namespace HAHATalk.Server.Repository
{
    public interface IChatRepository
    {

        // 채팅방 목록 가져오기 (비동기) 
        Task<List<ChatList>> MSSQL_GetChatListAsync(string email);

        // 2026.04.01 방 ID로 기존 메세지 내역 불러오기 
        Task<List<ChatMessage>> MSSQL_GetMessageByRoomIdAsync(string roomId);

        // 안 읽은 메세지 총합 가져오기 (비동기 방식) 
        Task<int> MSSQL_GetTotalUnreadCountAsync(string email);

        // 2026.04.01 새로운 DB에 저장하기 
        Task<bool> MSSQL_SaveMessageAsync(ChatMessage message);

        // 2026.04.01 채팅창 업데이트 
        Task<bool> MSSQL_UpdateChatListAsync(ChatMessageDto message, string targetId, string targetName, string myId, string myNickname);

        // 읽음상태 업데이트 
        Task<bool> MSSQL_UpdateReadStatusAsync(string roomId, string userId);

        // 읽음 처리 
        Task<bool> MarkAsReadAsync(string roomId, string userId);

        // 특정 채팅방의 현재 멤버 인원수 가져오기 
        Task<int> MSSQL_GetRoomMemberCountAsync(string roomId);

        // 새로운 멤버 추가 
        Task<bool> MSSQL_AddChatMemberAsync(string roomId, string userId);

        // 메세지 삭제 기능 추가 
        Task<bool> MSSQL_DeleteMessageAsync(string messageGuid);
    }
}
