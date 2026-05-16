using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Enums
{
    /// <summary>
    /// 2026.05.16 Add
    /// 회원가입 유효성 검증 항목들을 관리하는 서버/클라이언트 공용 에넘
    /// </summary>
    public enum SignupValidationType
    {
        None,
        Email,
        ExistEmail,
        Nickname,
        CellPhone,
        Password,
        PasswordConfirm,
        DifferentPassword
    }
}
