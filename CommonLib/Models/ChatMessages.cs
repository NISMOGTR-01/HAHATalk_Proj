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

        public string MessageGuid { get; set; } 

        // 전송 상태를 정의하는 enum 
        public enum MessageStatus
        {
            Sending = 0,    // 전송 중 
            Success = 1,    // 성공 
            Fail = 2,       // 실패
        }

        // DB의 SendState 컬럼과 mapping 
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Status))] // SendState가 바뀌면 Status 속성도 알림
        private int _sendState = (int)MessageStatus.Success;

        // UI 바인딩용 가독성 속성 
        public MessageStatus Status => (MessageStatus)SendState;

        // 2026.04.27 전송 중일 때 흐리게 / 성공 or 실패시 진하게 
        public double Opacity => Status == MessageStatus.Sending ? 0.6 : 1.0;

        // 전송 중일 때만 로딩 스피너 활성화 
        public bool IsLoading => Status == MessageStatus.Sending;

        // 전송 실패했을 때만 재시도 버튼 노출 
        public bool IsRetryVisible => Status == MessageStatus.Fail;

        // 전송 성공했을 때만 읽음 숫자(1) 노출 가능 
        public bool IsStatusSucess => Status == MessageStatus.Success;

        // 이미지 메시지 인지 여부를 즉시 판단하는 읽기 전용속성 
        public bool IsImage => MessageType == 1 && !string.IsNullOrEmpty(FilePath);

        // 텍스트 메세지인지 여부 
        public bool IsText => MessageType == 0;

        // View에서 Binding 읽기 전용 시간 포맷 
        // 오전 / 오후가 나오도록 반영하는 부분 
        public string FormattedTime => SendTime.ToString("t"); // "오전 11:30"
    }
}
