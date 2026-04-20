using CommonLib.Dtos;
using CommonLib.Models;
using MySqlX.XDevAPI.Common;
using System.Diagnostics;
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
    }
}
