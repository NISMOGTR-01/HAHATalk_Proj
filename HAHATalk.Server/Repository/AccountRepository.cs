using Dapper;
using CommonLib.Models;
using HAHATalk.Server.Repository;
using HAHATalk.Server.Security;
using Microsoft.Data.SqlClient;
using System.Data;
using Serilog;


namespace HAHATalk.Server.Repository
{
    public class AccountRepository : RepositoryBase, IAccountRepository
    {
        // 부모인 RepositoryBase에 IConfi
        public AccountRepository(IConfiguration configuration) : base(configuration) 
        {
        
        }
        
        public async Task<bool> MSSQL_ExistEmailAsync(string email)
        {
            const string query = "SELECT COUNT(1) FROM account WHERE email = @email";

            try
            {
                using var db = CreateConnection();
                int count = await db.ExecuteScalarAsync<int>(query, new { email });
                return count > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Account] 이메일 중복 체크 중 오류 발생 (Email: {Email})", email);
                return false;
            }
        }

        public async Task<string?> MSSQL_Find_AccountAsync(Account account)
        {
            string phoneNumber = account.CellPhone?.Replace("-", "") ?? "";
            const string query = "SELECT email FROM account WHERE cell_phone = @cell_phone";

            try
            {
                using var db = CreateConnection();
                // 단일 문자열 값만 가져올 때도 ExecuteScalar가 편합니다.
                var email = await db.ExecuteScalarAsync<string>(query, new { cell_phone = phoneNumber });
                
                // 0 대신 null을 반환하여 string? 취지에 맞게 변경 
                return email;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Account] 계정 찾기 중 오류 발생 (Phone: {Phone})", phoneNumber);
                return null;
            }
        }

        public async Task<Account?> MSSQL_GetAccountByEmailAsync(string email)
        {
            // DB 컬럼명과 모델 프로퍼티명을 맞추기 위해 Alias 사용 (pwd -> Pwd)
            const string query = @"
                SELECT email AS Email, 
                    nickname AS Nickname, 
                    pwd AS Pwd, 
                    profile_Img AS ProfileImg, 
                    status_Msg AS StatusMsg
                FROM account 
                WHERE email = @email";

            try
            {
                using var db = CreateConnection();
                // QueryFirstOrDefaultAsync는 결과가 없으면 null을 반환합니다.
                return await db.QueryFirstOrDefaultAsync<Account>(query, new { email });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Account] 계정 조회 중 오류 발생 (Email: {Email})", email);
                return null;
            }
        }

        public async Task<bool> MSSQL_Login_CheckAsync(string email, string pwd)
        {
            var account = await MSSQL_GetAccountByEmailAsync(email);

            if (account == null)
            {
                Log.Warning("[Account] 로그인 실패: 존재하지 않는 계정 (Email: {Email})", email);
                return false;
            }

            bool isSuccess = SecurityHelper.VerifyPassword(pwd, account.Pwd);

            if (!isSuccess)
                Log.Warning("[Account] 로그인 실패: 비밀번호 불일치 (Email: {Email})", email);
            else
                Log.Information("[Account] 로그인 성공 (Email: {Email})", email);

            return isSuccess;
        }

        public async Task<long> MSSQL_Pass_UpdateAsync(Account account, string changePwd)
        {
            // 변경할 비밀번호 암호화
            string hashedPwd = SecurityHelper.HashPassword(changePwd);
            const string query = "UPDATE account SET pwd = @pwd WHERE email = @email";

            try
            {
                using var db = CreateConnection();
                int rows = await db.ExecuteAsync(query, new { pwd = hashedPwd, email = account.Email });

                if (rows > 0)
                    Log.Information("[Account] 비밀번호 변경 완료 (Email: {Email})", account.Email);

                return rows;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Account] 비밀번호 변경 중 오류 발생 (Email: {Email})", account.Email);
                return 0;
            }
        }

        public async Task<long> MSSQL_SaveAsync(Account account, string changPwd)
        {
            // 회원 가입시 비밀번호 암호화 
            // parameter로 받은 changePwd가 있다면 그것을,없다면 account.Pwd를 암호화 
            string targetPwd = !string.IsNullOrEmpty(changPwd) ? changPwd : account.Pwd;
            string hashedPwd = SecurityHelper.HashPassword(targetPwd);

            // 저장할 때도 하이픈을 제거하여 dataformat 통일 
            string clearnPhone = account.CellPhone?.Replace("-", "") ?? "";

            const string query = @"
                INSERT INTO account (pwd, email, nickname, cell_phone)
                VALUES (@hashedPwd, @Email, @Nickname, @CellPhone);";

            try
            {
                using var db = CreateConnection();
                // Dapper는 익명 객체 프로퍼티와 쿼리의 @파라미터를 매핑합니다.
                int rows = await db.ExecuteAsync(query, new
                {
                    hashedPwd,
                    account.Email,
                    account.Nickname,
                    CellPhone = clearnPhone
                });

                if (rows > 0)
                    Log.Information("[Account] 신규 회원가입 완료 (Email: {Email})", account.Email);

                return rows;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Account] 회원가입 처리 중 오류 발생 (Email: {Email})", account.Email);
                return 0;
            }
        }

        // 프로필 이미지와 상태 메세지를 통합 관리하는 메서드 
        public async Task<bool> MSSQL_UpdateProfileImageAsync(string? email, string? imagePath, string? statusMsg)
        {
            // imagePath는 서버에 저장된 상대경로 (:updalods/profiles/556.jpg
            const string query = @"
                UPDATE account 
                SET profile_img = ISNULL(@imagePath, profile_img), 
                    status_msg = ISNULL(@statusMsg, status_msg)
                WHERE email = @email
                ";

            try
            {
                using var db = CreateConnection();
                // dapper를 이용해 파라미터 mapping 
                int rows = await db.ExecuteAsync(query, new { email, imagePath, statusMsg });

                if(rows > 0)
                {
                    Log.Information("[Account] 프로필 정보 업데이트 완료 (Email : {Email}", email);
                }

                return rows > 0;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "[Account] 프로필 업데이트 중 예외 발생 (Email : {Email|}", email);
                return false;
            }
        }
    }
}
