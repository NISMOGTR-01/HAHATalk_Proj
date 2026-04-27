namespace CommonLib.Dtos
{
    public class ChatMessageDto
    {
        // 1.방 정보 및 발신자 
        public string RoomId { get; set;  } = string.Empty;
        public string SenderId { get; set;  } = string.Empty;

        // 2.메세지 내용 및 타입 
        public string Message {  get; set; } = string.Empty;
        public int MessageType { get; set; } // 0 .Text 1.Image

        // 3.시간 (서버에서 기록하지만 클라이언트 시간 전달) 
        public DateTime SendTime { get; set;  }

        // 4.파일관련 (이미지 / 파일 전송 시) 
        public string? FilePath { get; set;  }
        public string? FileName { get; set; }

        // UI에서 발신자 이름을 띄우기 위한 속성 
        public string SenderName { get; set; } = string.Empty;

        // 내 메시지인지 구분하는 플래그 (DataTrigger용) 
        public bool IsMine {  get; set; }

        // 중복 방지 및 메세지 식별용 고유 ID 
        public string MessageGuid { get; set; } = string.Empty;

        // 시간 포맷팅 
        public string FomattedTime => SendTime.ToString("t");
        
    }
}
