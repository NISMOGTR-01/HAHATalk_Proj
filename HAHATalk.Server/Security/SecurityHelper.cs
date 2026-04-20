
using BCrypt.Net;

namespace HAHATalk.Server.Security
{
    public class SecurityHelper
    {
        // BCrypt 작업 부하 (Work Factor) 설정 
        // 숫자가 높을 수록 암호화가 강력해지지만 서버 리소스를 더 많이 사용(기본값 11 or 12 권장) 
        private const int WorkFactor = 11;

        // 사용자가 입력한 평문 비밀번호를 BCypt 알고리즘으로 해싱 
        // 회원 가입 
        // parameter = rawPassword : 사용자가 실제로 입력한 비밀번호 
        // return : 암호화된 해시 문자열 (Salt 포함) 
        public static string HashPassword(string rawPassword)
        {
            if (string.IsNullOrWhiteSpace(rawPassword))
            {
                return String.Empty;
            }

            // BCrypt는 실행될 때마다 내부적으로 새로운 Salt를 생성하여 해싱 
            return BCrypt.Net.BCrypt.HashPassword(rawPassword, WorkFactor);
        }

        // 사용자가 입력한 비밀번호와 DB에 저장된 해시값을 비교 검증 
        // 로그인시 사용 
        // inputPassword : 로그인 시 입력한 평문 비밀번호 
        // hashedPassword : DB (Account 테이블)에 저장되어 있던 해시값 
        // returns 일치여부 : (true / false) 
        public static bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            if(string.IsNullOrEmpty(inputPassword) || string.IsNullOrWhiteSpace(hashedPassword))
            {
                return false;
            }

            try
            {
                // BCrypt.Verify가 내부적으로 Salt를 추출하여 입력된 비밀번호와 대조 
                return BCrypt.Net.BCrypt.Verify(inputPassword, hashedPassword);
            }
            catch
            {
                // 해시 형식이 잘못 되었거나 오류 발생시 인증 실패 처리 
                return false;
            }
        }
    }
}
