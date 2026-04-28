
using CommonLib.Models;

namespace HAHATalk.Server.Repository
{
    public interface IAccountRepository
    {
        // 로그인 체크 
        Task<bool> MSSQL_Login_CheckAsync(string email, string pwd);

        // 이메일 존재 여부 확인 
        Task<bool> MSSQL_ExistEmailAsync(string email);

        // 회원가입 정보 저장 
        Task<long> MSSQL_SaveAsync(Account account, string changPwd);

        // 비밀번호_수정 
        Task<long> MSSQL_Pass_UpdateAsync(Account account, string changePwd);

        // 전화번호로 계정 찾기 
        Task<string?> MSSQL_Find_AccountAsync(Account account);

        // 이메일로 상세 정보 가져오기 
        Task<Account?> MSSQL_GetAccountByEmailAsync(string email);

        // 프로필 이미지 경로 업데이트 
        Task<bool> MSSQL_UpdateProfileImageAsync(string? email, string? imagePath, string? statusMsg);
    }
}
