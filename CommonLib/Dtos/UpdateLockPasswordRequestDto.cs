using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Dtos
{
    /// <summary>
    /// 2026.05.16 Add
    /// 잠금 암호 설정 및 변경 요청 파라미터 바인딩용 DTO 클래스
    /// </summary>
    public class UpdateLockPasswordRequestDto
    {
        /// <summary>
        /// 잠금 설정을 변경할 사용자의 이메일 계정
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 설정할 4자리 숫자 잠금 비밀번호
        /// </summary>
        public string LockPassword { get; set; } = string.Empty;

    }
}
