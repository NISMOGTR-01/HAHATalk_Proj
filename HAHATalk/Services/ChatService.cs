using CommonLib.Dtos;
using CommonLib.Enums;
using CommonLib.Models;
using System.IO;
using System.IO.Packaging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

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

        // 채팅 리스트 가져오기 
        public async Task<List<ChatList>> GetChatListAsync(string email)
        {
            try
            {
                var list = await _httpClient.GetFromJsonAsync<List<ChatList>>(
                    $"{BaseUrl}/list/{email}") ?? new();

                foreach(var item in list)
                {
                    // 서버 API가 User 테이블과 JOIN해서 ProfileImg를 넣어줌 
                    if(!string.IsNullOrEmpty(item.ProfileImg))
                    {
                        item.ProfileImg = GetServerFullUrl(item.ProfileImg);
                    }
                }

                return list;
            }
            catch
            {
                return new List<ChatList>();
            }
        }

        // 채팅 History 가져오기 
        public async Task<List<ChatMessage>> GetChatHistoryAsync(string roomId)
        {
            try
            {
                // 1. 서버에서 히스토리 목록을 가져옵니다.
                var messages = await _httpClient.GetFromJsonAsync<List<ChatMessage>>($"{BaseUrl}/history/{roomId}") ?? new();

                // 2. 재로그인 시 이미지가 보이도록 경로를 조립합니다.
                foreach (var msg in messages)
                {
                    // [수정] 이미지(2) 외에 동영상(3), 파일(4) 등 파일 경로가 포함된 모든 타입 처리
                    if (!string.IsNullOrEmpty(msg.FilePath) && msg.FilePath.StartsWith("/uploads"))
                    {
                        msg.FilePath = GetServerFullUrl(msg.FilePath);
                    }

                    if (!string.IsNullOrEmpty(msg.SenderProfile) && msg.SenderProfile.StartsWith("/uploads"))
                    {
                        msg.SenderProfile = GetServerFullUrl(msg.SenderProfile);
                    }
                }

                return messages;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"히스토리 로드 실패: {ex.Message}");
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
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/save", message);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"@@@ DB 저장 실패: {error}");
                }
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
                    SenderName = myNickname,            // 발신자 닉네임 명시
                    SenderProfile = message.SenderProfile,  // 프로필 이미지 경로 누락 주의
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
                using var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read);
                using var fileContent = new StreamContent(fileStream);

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

            // HttpClient에 설정된 BaseAddress를 가져옵니다.
            var baseAddr = _httpClient.BaseAddress?.ToString().TrimEnd('/');

            // 상대 경로가 /로 시작하지 않으면 붙여줍니다.
            var path = relativeUrl.StartsWith("/") ? relativeUrl : "/" + relativeUrl;

            return $"{baseAddr}{path}";
        }

        public async Task<FileUploadResponseDto> UploadFileExtendedAsync(string localFilePath)
        {
            try
            {
                if (!File.Exists(localFilePath)) return null;

                // BaseUrl은 "api/Chat"이므로 "/upload-file"만 붙여주면 됩니다.
                var requestUrl = $"{BaseUrl}/upload-file";

                using var content = new MultipartFormDataContent();
                // [수정] StreamContent가 사용하는 Stream을 using으로 묶어 확실히 해제
                using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                using var fileContent = new StreamContent(fileStream);

                content.Add(fileContent, "file", Path.GetFileName(localFilePath));

                // [수정] PatchAsync -> PostAsync (서버 컨트롤러가 Post이므로)
                var response = await _httpClient.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    // ReadFromJsonAsync는 기본적으로 대소문자를 구분하지 않아 안전합니다.
                    return await response.Content.ReadFromJsonAsync<FileUploadResponseDto>();
                }

                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[UploadFileExtended] 서버 에러: {response.StatusCode}, {error}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UploadFileExtended] 예외 발생: {ex.Message}");
            }
            return null;
        }

        // 응답을 받기 위한 내부 클래스 
        public class UploadResponse { public string Url { get; set; } }
    }
}
