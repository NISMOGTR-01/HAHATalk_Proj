using CommonLib.Models;
using System.Net.Http;
using System.Net.Http.Json;

namespace HAHATalk.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/Chat";

        public ChatService(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public async Task<List<ChatList>> GetChatListAsync(string email)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ChatList>>(
                    $"{BaseUrl}/list/{email}") ?? new();
            }
            catch
            {
                return new List<ChatList>();
            }
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(string roomId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ChatMessage>>($"{BaseUrl}/history/{roomId}") ?? new();
            }
            catch
            {
                return new List<ChatMessage>();
            }
        }

        public async Task<int> GetTotalUnreadCountAsync(string email)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<int>($"{BaseUrl}/unread-total/{email}");
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> SaveMessageAsync(ChatMessage message)
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/save", message);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateChatListAsync(ChatMessage message,
             string targetId,
            string targetName, string myId, string myNickname)
        {
            // 서버 컨트롤러의 update-list 액션으로 데이터를 내보내기
            // 익명 객체를 활용하여 서버가 기대하는 parameter 구조 맞추기 
            var updateData = new
            {
                // 서버의 Dto (ChatUpdateDto)와 일치 2026.04.15
                Message = new
                {
                    RoomId = message.RoomId,
                    SenderId = message.SenderId,
                    Message = message.Message,
                    MessageType = message.MessageType,
                    SendTime = message.SendTime,
                    FilePath = message.FilePath,
                    FileName = message.FileName,
                    MessageGuid = message.MessageGuid
                },                

                TargetId = targetId,
                TargetName = targetName,
                MyId = myId,
                MyNickname = myNickname,
            };

            // 호출 경로(BaseUrl)가 컨트롤러의 [Route]와 맞는지 다시 한번 확인!
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/update-list", updateData);

            if (!response.IsSuccessStatusCode)
            {
                // 에러 발생 시 서버가 보내준 구체적인 이유를 출력해서 확인 (디버깅용)
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Update-List 실패 사유: {error}");
            }

            return response.IsSuccessStatusCode;
        }

        public async Task MarkAsReadAsync(string roomId, string userId)
        {
            var request = new {RoomId = roomId, UserId = userId};
            await _httpClient.PostAsJsonAsync("api/chat/mark-read", request);
        }
    }
}
