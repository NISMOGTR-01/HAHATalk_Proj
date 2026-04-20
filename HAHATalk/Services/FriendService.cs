using CommonLib.Dtos;
using CommonLib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace HAHATalk.Services
{
  
    public class FriendService : IFriendService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "api/Friend";

        public FriendService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Friend>> GetFriendsAsync(string myId)
        {
            try
            {
                // 클라이언트 코드 수정 (2026.04.11)
                var response = await _httpClient.GetFromJsonAsync<List<Friend>>($"{BaseUrl}/list/{myId}");

                Debug.WriteLine($"서버 응답 데이터: {response}");

                return response ?? new List<Friend>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetFriendsAsync Error: {ex.Message}");
                return new List<Friend>();
            }
        }


        public async Task<bool> AddFriendAsync(string myId, string friendEmail, string friendName, string statusMsg)
        {
            try
            {
                // DTO 생성 
                var dto = new AddFriendRequestDto
                {
                    MyEmail = myId,
                    TargetEmail = friendEmail,
                    FriendName = friendName,
                    StatusMessage = statusMsg
                };

                // 서버에 POST 요청 전송 
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/add", dto);

                // 성공 여부 확인 
                if (!response.IsSuccessStatusCode)
                {
                    // 서버에서 보낸 에러 메세지를 읽어옴 
                    var errorMessage = await response.Content.ReadAsStringAsync();

                    // 디버그 창에 출력 (개발용 테스트) 
                    System.Diagnostics.Debug.WriteLine($"[친구 등록 실패] 서버 응답 : {errorMessage}");

                    // 구체적인 에러 내용을 담아 예외 발생 -> ViewModel의 catch로 전달 
                    throw new Exception(string.IsNullOrWhiteSpace(errorMessage) ? "친구 추가 중 오류발생 " : errorMessage);
                }
                
                return true;
            }
            catch (HttpRequestException httpEx)
            {
                // 네트워크 연결 문제 (인터넷 끊김, 서버 꺼짐 등)
                System.Diagnostics.Debug.WriteLine($"[네트워크 오류]: {httpEx.Message}");
                throw new Exception("서버WriteLine와 통신할 수 없습니다. 네트워크 상태를 확인해주세요.");
            }
            catch (Exception ex) 
            {
                // 그 외 모든 예외 처리
                System.Diagnostics.Debug.WriteLine($"[AddFriendAsync 예외 발생]: {ex.Message}");

                // 다시 던져서(re-throw) ViewModel에서 MessageBox 등을 띄울 수 있게 함
                throw;
            }
        }


        public async Task<bool> IsFriendAlreadyExistsAsync(string myId, string friendEmail)
        {
            // 2026.04.14 : GetFromJson -> GetAsync (응답상태를 먼저 확인, 데이터를 읽어옴) 

            try
            {
                // 쿼리 스트링 방식으로 호출하거나, 서버 설계에 따라 경로를 맞춥니다.
                var response = await _httpClient.GetAsync($"{BaseUrl}/exists/{myId}/{friendEmail}");
                
                if(response.IsSuccessStatusCode)
                {
                    // 문자열로 읽어서 bool로 변환 
                    var content = await response.Content.ReadAsStringAsync();

                    if(bool.TryParse(content, out bool result))
                    {
                        return result;
                    }
                }

                return false;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IsFriendAlreadyExistsAsync Error: {ex.Message}");
                return false;
            }
        }
    }
}
