using CommonLib.Dtos;
using CommonLib.Models;
using System.Threading.Tasks;

namespace HAHATalk.Services
{
    public interface IAccountService
    {
        // 서버 API에 로그인 요청 
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto);

        // 서버 API에 회원가입 요청 
        Task<bool> RegisterAsync(Account newAccount);

        // 특정 이메일의 정보를 서버에서 가져오기 
        Task<Account?> GetAccountAsnyc(string email);

        // 비밀번호 변경 
        Task<bool> UpdatePasswordAsync(PasswordUpdateDto updateDto);
        //Task<bool> UpdatePasswordAsync(string email, string oldPwd, string newPwd);

        // 이메일 중복 체크용 인터페이스 
        Task<bool> ExistEmailAsync(string email);

        // 계정찾기 비동기 메서드 
        Task<string?> FindAccountAsync(Account account);

        // 프로필 정보 (이미지 파일 경로 및 상태 메세지) 업데이트 
        // 이미지 경로는 로컬 파일 경로를 받아 서비스 내부에서 스트림으로 변환 처리 
        Task<bool> UpdateProfileAsync(string email, string? filePath, string? statusMsg);
    }
}
