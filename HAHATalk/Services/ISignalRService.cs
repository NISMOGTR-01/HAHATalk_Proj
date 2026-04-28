using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;


namespace HAHATalk.Services
{
    public interface ISignalRService
    {
        // 2026.03.26 
        // 서버 연결 상태 확인 
        HubConnectionState State { get; }

        // 서버 연결 및 해제 
        Task ConnectAsync();
        Task DisconnectAsync();

        // 메시지 전송 (상대방 이메일, 메시지 내용) 
        Task SendMessageAsync(string roomId, string targetEmail, string message);

        // 실시간 메시지 수신 이벤트 (보낸사람 이메일, 메시지 내용) 
        event Action<string, string> MessageReceived;

        Task JoinRoom(string roomId);

        Task SendReadReceiptAsync(string roomId, string targetId);
    }
}
