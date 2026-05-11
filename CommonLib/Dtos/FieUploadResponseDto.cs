using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Dtos
{
    public class FileUploadResponseDto
    {
        // 서버에서 저장된 상대 경로 (예: /uploads/chat/images/uuid.png)
        public string Url { get; set; }

        // 서버에서 판별한 메시지 타입 (이미지: 2, 비디오: 3, 파일: 4 등)
        public int MessageType { get; set; }

        // 원본 파일 이름
        public string OriginName { get; set; }
    }
}
