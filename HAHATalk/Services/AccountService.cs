using CommonLib.Dtos;
using CommonLib.Models;
using MySqlX.XDevAPI.Common;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HAHATalk.Services
{
    public class AccountService : IAccountService
    {
        private readonly HttpClient _httpClient;

        // 서버 주소 (나중에 
        private const string BaseUrl = "api/Account";

        public AccountService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Account?> GetAccountAsnyc(string email)
        {
            // GET / Api/Account/{email} 호출
            return await _httpClient.GetFromJsonAsync<Account>($"{BaseUrl}/{email}");
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto)
        {
            try
            {
                // POST / api /Account/login 호출 
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", loginDto);
                //var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/login", loginInfo);

                return await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"로그인 통신 실패 : {ex.Message}");
                return new LoginResponseDto
                {
                    IsSuccess = false, Message = "서버 연결에 실패했습니다."
                };
            }

            
     
           
        }

        public async Task<bool> RegisterAsync(Account newAccount)
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/register", newAccount);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdatePasswordAsync(PasswordUpdateDto updateDto)
        {
            // PasswordUpdateDto 생성 
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/update-password", updateDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ExistEmailAsync(string email)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<bool>($"{BaseUrl}/exist/{email}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"이메일 체크 실패: {ex.Message}");
                return true;
            }
        }

        public async Task<string?> FindAccountAsync(Account account)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/find-account", account);

                if (response.IsSuccessStatusCode)
                {
                    // 서버 응답 읽기 
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"계정 찾기 실패: {ex.Message}");
            }

            return "0";
        }

        // 프로필 업데이트 함수 
        public async Task<bool> UpdateProfileAsync(string email, string? filePath, string? statusMsg)
        {
            try
            {
                // 파일 업로드를 위한 Multipart Content 생성 
                using var content = new MultipartFormDataContent();

                // 이메일 
                content.Add(new StringContent(email), "email");

                // 상태 메시지 
                if(!string.IsNullOrEmpty(statusMsg))
                {
                    content.Add(new StringContent(statusMsg), "statusMsg");
                }

                // 이미지 파일 
                if(!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var fileContent = new StreamContent(fileStream);

                    // 이미지 타입에 맞춰 Header 설정 
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                    // 서버 컨트롤러의 [FromForm] IFormFile file 파라미터 이름과 일치해야 함 
                    content.Add(fileContent, "file", Path.GetFileName(filePath));
                }

                // POST /api/Account/upload-profile 호출 
                var response = await _httpClient.PostAsync($"{BaseUrl}/upload-profile", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"프로필 업데이트 실패: {ex.Message}");
                return false;
            }
        }
    }
}
