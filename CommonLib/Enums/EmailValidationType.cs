using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Enums
{
    /// <summary>
    /// 2026.05.16 Add
    /// 이메일 실시간 유효성 검증 상태를 관리하는 서버/클라이언트 공용 에넘
    /// </summary>
    public enum EmailValidationType
    {
        None,
        AlreadyExists,
        FormatError
    }
}
