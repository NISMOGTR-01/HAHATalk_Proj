using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Text;

namespace CommonLib.Models
{

    [ObservableObject]
    public partial class ChatMessage
    {
        // 2026.04.01 RoomId 추가 (어떤 방의 메세지인지 구분하는 기준) 
        [ObservableProperty]
        private string _roomId;
        [ObservableProperty]
        private string _senderId;
        [ObservableProperty]
        private DateTime _sendTime;
        [ObservableProperty]
        private string _message;
        [ObservableProperty]
        private int _messageType; // 0: 텍스트, 1: 이미지 등 (확장용)

        // 2026.04.07 
        [ObservableProperty]
        private string _filePath; // 파일 저장 경로 
        [ObservableProperty]
        private string _fileName; // 파일 이름 

        // 상태 값
        [ObservableProperty]
        private bool _isRead; // 읽음 확인용
        // UI 전용 필드 
        [ObservableProperty]
        private bool _isMine;

        [ObservableProperty]
        private string _senderName;
        [ObservableProperty]
        private string _profilePath;

        // 이미지 메시지 인지 여부를 즉시 판단하는 읽기 전용속성 
        public bool IsImage => MessageType == 1 && !string.IsNullOrEmpty(FilePath);

        // 텍스트 메세지인지 여부 
        public bool IsText => MessageType == 0;

        // View에서 Binding 읽기 전용 시간 포맷 
        // 오전 / 오후가 나오도록 반영하는 부분 
        public string FormattedTime => SendTime.ToString("t"); // "오전 11:30"
    }
}
