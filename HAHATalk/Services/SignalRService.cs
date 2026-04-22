using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using HAHATalk.Stores;
using System.CodeDom;
using CommunityToolkit.Mvvm.Messaging;
using CommonLib.Models;
using CommonLib.Dtos;
using HAHATalk.Messages;

namespace HAHATalk.Services
{
    // 서버와 연결을 맺고 , 메시지를 실시간으로 주고 받는 싱글톤 서비스 
    public class SignalRService : ISignalRService
    {
        private readonly HubConnection _connection;
        private readonly UserStore _userStore;

        // 이벤트 정의 
        public event Action<string, string>? MessageReceived;

        public SignalRService(UserStore userStore)
        {
            _userStore = userStore;

            // 허브 연결 설정 (서버 주소는 환경에 맞게 수정) 
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7203/ChatHub", options =>
                {
                    // 필요 시 AccessToken 등을 헤더에 담을 수 있습니다.
                    // options.AccessTokenProvider = () => Task.FromResult(_userStore.Token);
                })
                .WithAutomaticReconnect() // 자동 재연결 활성화!
                .Build();

            // 서버에서 ChatMessageDto 객체 하나를 던질 경우를 대비한 리스너 
            _connection.On<ChatMessageDto>("ReceiveMessage", (msgDto) =>
            {
                // 기존 이벤트 방식 지원 (문자열 두개로 분해해서 전달) 
                MessageReceived?.Invoke(msgDto.SenderId, msgDto.Message);

                // Messenger를 통해 현재 열려 잇는 ChatRoomViewModel 에 직접 전달 
                App.Current.Dispatcher.Invoke(() =>
                {
                    WeakReferenceMessenger.Default.Send(new NewMessageReceivedMessage(msgDto));
                });
            });

            // [수정!!!] 기존 문자열 두 개 방식 리스너 (하위 호환성 유지)
            _connection.On<string, string>("ReceiveMessageLegacy", (senderEmail, message) =>
            {
                MessageReceived?.Invoke(senderEmail, message);
            });

            // 친구 추가 알림 수신 리스터 (서버의 SendAsync("UpdateChatList")를 받는다) 
            _connection.On("UpdateChatList", () =>
            {
                System.Diagnostics.Debug.WriteLine("SignalR: 상대방이 나를 친구 추가하여 친구목록을 갱신합니다.");

                // 메신저를 통해 ChatListControlViewModel에게 즉시 새로고침 신호 전송 
                // UI 스레드에서 돌아가도록 처리 
                App.Current.Dispatcher.Invoke(() =>
                {
                    WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());
                });
            });

            // 서버에서 "누군가 메세지를 읽었음"신호를 보낼때 
            _connection.On<string>("ReceiveReadReceipt", (roomId) =>
            {
                // Messenger를 통해 ChatRoomViewModel에 알림 
                WeakReferenceMessenger.Default.Send(new MessagesReadMessage(roomId));
            });
        }

        // 현재 연결상태 반환 
        public HubConnectionState State => _connection.State;

        // 연결시작 
        public async Task ConnectAsync()
        {
            try
            {
                // 연결이 끊겨 있을 때만 StartAsync 호출 
                if(_connection.State == HubConnectionState.Disconnected)
                {
                    await _connection.StartAsync();
                    System.Diagnostics.Debug.WriteLine("Signal R Connected!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write
                (
                    $"Signal R Connection Error: {ex.Message}"
                );
            }
        }

        // 연결 종료 
        public async Task DisconnectAsync()
        {
            if(_connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync();
            }
        }

        // 메시지 전송 로직 
        public async Task SendMessageAsync(string roomId, string targetEmail, string message)
        {
            if(_connection.State != HubConnectionState.Connected)
            {
                // 연결이 끊긴 경우 다시 시도 
                await ConnectAsync();
            }
            try
            {
                // roomId 추가 
                // 서버의 ChatHub.SendMessage(roomId, senderId, targetId, message) 호출
                //
                await _connection.InvokeAsync("SendMessage", 
                    roomId,
                    _userStore.CurrentUserEmail, 
                    targetEmail, 
                    message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine
                (
                    $"Signal R Send Error : {ex.Message}"
                );
            }
        }

        // 채팅방 입장 
        public async Task JoinRoom(string roomId)
        {
            if (_connection.State != HubConnectionState.Connected)
            {
                // 연결이 안된 경우 연결 부터??
                await ConnectAsync();
            }

            try
            {
                // 서버 ChatHub에 있는 public async Task JoinRoom(string roomId) 호출 
                await _connection.InvokeAsync("JoinRoom", roomId);
                System.Diagnostics.Debug.WriteLine($"SignalR: Joined Room {roomId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JoinRoom Error: {ex.Message}");
            }
        }

        // 메세지 읽음 알리는 메서드 
        public async Task SendReadReceiptAsync(string roomId, string targetId)
        {
            if(_connection.State != HubConnectionState.Connected)
            {
                try
                {
                    // 서버 Hub의 SendReadReceipt 메서드 호출 
                    await _connection.InvokeAsync("SendReadReceipt", roomId, targetId);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"읽음 신호 전송 실패: {ex.Message}");
                }
            }
        }
    }


}
