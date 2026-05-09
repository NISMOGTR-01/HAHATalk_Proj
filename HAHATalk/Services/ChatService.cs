using CommonLib.Enums;
using CommonLib.Models;
using System.IO;
using System.IO;
using System.IO.Packaging;
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
                // 1. 서버에서 히스토리 목록을 가져옵니다.
                var messages = await _httpClient.GetFromJsonAsync<List<ChatMessage>>($"{BaseUrl}/history/{roomId}") ?? new();

                // 2. [핵심] 재로그인 시 이미지가 보이도록 경로를 조립합니다.
                foreach (var msg in messages)
                {
                    // 타입이 이미지(1)이고, 경로가 DB에 저장된 상대경로(/uploads...)라면
                    if (msg.MessageType == (int)ChatMessageTypes.Image &&
                        !string.IsNullOrEmpty(msg.FilePath) &&
                        msg.FilePath.StartsWith("/uploads"))
                    {
                        // GetServerFullUrl을 호출하여 완전한 주소(http://...)로 변환합니다.
                        msg.FilePath = GetServerFullUrl(msg.FilePath);
                    }
                }

                return messages;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"히스토리 로드 실패: {ex.Message}");
                return new List<ChatMessage>();
            }

            /*
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ChatMessage>>($"{BaseUrl}/history/{roomId}") ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"히스토리 로드 실패: {ex.Message}");
                return new List<ChatMessage>();
            }
            */
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
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/save", message);
                return response.IsSuccessStatusCode;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"메세지 저장 에러 : {ex.Message}");
                return false;
            }
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
            try
            {
                var request = new { RoomId = roomId, UserId = userId };
                // BaseUrl 변수를 사용하여 경로 일관성 유지 
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/mark-read", request);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"읽음 처리 서버 응답 에러 : {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"읽음 처리 통신 에러: {ex.Message}");
            }
        }

        public async Task<string> UploadFileAsync(string localPath)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                // 파일을 읽어서 스트림으로 변환 한다 
                var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
                var fileContent = new StreamContent(fileStream);

                // 서버의 IFormFile "file"과 이름 일치시기키 
                content.Add(fileContent, "file", Path.GetFileName(localPath));

                // 서버의 ChatControll에 만든 upload-file 호출 
                var response = await _httpClient.PostAsync($"{BaseUrl}/upload-file", content);

                if(response.IsSuccessStatusCode)
                {
                    // 서버가 반환한 { url : "/uploads/chat/images/..."} 파싱 
                    var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
                    return result?.Url;
                }

                return null;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"파일 업로드 실패:{ex.Message}");
                return null; 
            }
        }

        public string GetServerFullUrl(string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl)) return string.Empty;

            // HttpClient에 설정된 BaseAddress(예: https://127.0.0.1:7119/)를 가져옵니다.
            var baseAddr = _httpClient.BaseAddress?.ToString().TrimEnd('/');

            // 상대 경로가 /로 시작하지 않으면 붙여줍니다.
            var path = relativeUrl.StartsWith("/") ? relativeUrl : "/" + relativeUrl;

            return $"{baseAddr}{path}";
        }

        // 응답을 받기 위한 내부 클래스 
        public class UploadResponse { public string Url { get; set; } }
    }
}
