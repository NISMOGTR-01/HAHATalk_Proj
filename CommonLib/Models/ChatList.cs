using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Models
{
   
    public class ChatList
    {
        public string RoomId { get; set; } = string.Empty; // DB 채팅방 고유 ID
        public string TargetName { get; set; } = string.Empty; // 상대방 이름 (또는 그룹명)
        public string LastMessage { get; set; } = string.Empty; // 마지막 메세지 내용 (미리보기) 
        public DateTime LastTime { get; set; } // 마지막 메시지 수신 시간 
        public int UnreadCount { get; set;  } // 안 읽은 메시지 개수
        public string ProfileImg { get; set; } // 프로필 이미지 경로 

        // Signal R 장착 할 경우 추가 정보 (ex: 상대방 ID) 
        public string TargetId { get; set; } = string.Empty;
     }
}
