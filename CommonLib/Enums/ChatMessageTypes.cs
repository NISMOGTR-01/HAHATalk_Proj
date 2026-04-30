using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Enums
{
    public enum ChatMessageTypes
    {
        Text        = 0,    // 일반 텍스트 
        Image       = 1,    // 이미지 (jpg, png, gif등) 
        Video       = 2,    // 영상 (mp4등) 
        File        = 3,    // 일반 파일 
        Emoticon    = 4,    // 이모티콘 
        System      = 5     // 시스템 메시지 (입장, 퇴장등) 
    }
}
