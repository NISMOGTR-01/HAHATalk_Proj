using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace CommonLib.Dtos
{
    public class AddFriendRequestDto
    {
        // 내 ID (친구를 추가하는 사람의 이메일) 
        public string MyEmail { get; set; } = string.Empty;

        // 상대방 ID (Account 테이블에 실존해야 하는 이메일) 
        public string TargetEmail { get; set; } = string.Empty;

        // 내가 설정할 친구의 별명 
        public string FriendName { get; set; } = string.Empty;

        // 상태메세지 (필요시 전달) 
        public string StatusMessage { get; set; } = string.Empty;
    }
}
