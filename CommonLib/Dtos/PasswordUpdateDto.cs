using Org.BouncyCastle.Asn1.Mozilla;

namespace CommonLib.Dtos
{
    // 비밀번호 변경 시 클라이언트 <> 서버간의 데이터를 주고 받기 위한 전용 택배 

    public class PasswordUpdateDto
    {
        // 클라이언트에서 "email"로 보내도 ASP.NET Core가 "Email"로 구분해서 
        public string Email { get; set; } = string.Empty;

        // 현재 비밀번호 - BCrypt 검증용 
        public string OldPassword { get; set; } = string.Empty;

        // 새롭게 변경할 비밀번호 - Bcrypt 암호화 후 DB 저장용 
        public string NewPassword { get; set; } = string.Empty;
    }
}
