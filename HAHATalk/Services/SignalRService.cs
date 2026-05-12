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
        private readonly ApiSettings _apiSettings; // 설정 클래스 추가 

        // 이벤트 정의 
        public event Action<string, string>? MessageReceived;

        public SignalRService(UserStore userStore, ApiSettings apiSettings)
        {
            _userStore = userStore;
            _apiSettings = apiSettings;

            // 허브 연결 설정 (서버 주소는 환경에 맞게 수정) 
            _connection = new HubConnectionBuilder()
                .WithUrl(_apiSettings.ChatHubUrl, options =>
                {
                    // 필요 시 AccessToken 등을 헤더에 담을 수 있다.
                    // options.AccessTokenProvider = () => Task.FromResult(_userStore.Token);
                })
               .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) }) // 재연결 간격 구체화
                .Build();


            // 리스너 등록 조직 
            RegisterHubEvents();
        }

        private void RegisterHubEvents()
        {
            // 서버에서 ChatMessageDto 객체 하나를 던질 경우를 대비한 리스너 
            _connection.On<ChatMessageDto>("ReceiveMessage", (msgDto) =>
            {
                // 기존 이벤트 방식 지원 (문자열 두개로 분해해서 전달) 
                //MessageReceived?.Invoke(msgDto.SenderId, msgDto.Message); (2026.05.13 주석처리) 메세지 중복 


                WeakReferenceMessenger.Default.Send(new NewMessageReceivedMessage(msgDto));

            });

            // 친구 추가 알림 수신 리스터 (서버의 SendAsync("UpdateChatList")를 받는다) 
            _connection.On("UpdateChatList", () =>
            {
                // 메신저를 통해 ChatListControlViewModel에게 즉시 새로고침 신호 전송                 
                WeakReferenceMessenger.Default.Send(new RefreshChatListMessage());

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
                if (_connection.State == HubConnectionState.Disconnected)
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
            if (_connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync();
            }
        }

        // 연결 보장 메서드 (내부용) 
        private async Task EnsureConnected()
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await ConnectAsync();
            }
        }

        // 메시지 전송 로직 
        public async Task SendMessageAsync(string roomId, string targetEmail, string message)
        {
            await EnsureConnected();
            try
            {
                await _connection.InvokeAsync("SendMessage", roomId, _userStore.CurrentUserEmail, targetEmail, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR Send Error: {ex.Message}");
                throw; // ViewModel에서 전송 실패(Fail) 상태를 처리할 수 있도록 throw 권장
            }
        }

        public async Task JoinRoom(string roomId)
        {
            await EnsureConnected();
            try
            {
                await _connection.InvokeAsync("JoinRoom", roomId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JoinRoom Error: {ex.Message}");
            }
        }

        public async Task SendReadReceiptAsync(string roomId, string targetId)
        {
            // 읽음 신호는 실패해도 채팅 흐름에 치명적이지 않으므로 단순 체크만
            if (_connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendReadReceipt", roomId, targetId, _userStore.CurrentUserId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"읽음 신호 전송 실패: {ex.Message}");
                }
            }
        }

        public async Task SendFileMessageAsync(string roomId, string targetEmail, string fileName, string filePath, int messageType)
        {
            await EnsureConnected();

            try
            {
                // 서버 Hub의 SendFileMessage 메서드를 호출 
                await _connection.InvokeAsync("SendFileMessage", 
                    roomId,
                    _userStore.CurrentUserId,
                    targetEmail, 
                    fileName, 
                    filePath, 
                    messageType
                    );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR File Send Error: {ex.Message}");
            }
        }




        public async Task SendDtoMessageAsync(ChatMessageDto dto, string targetEmail)
        {
            await EnsureConnected();
            
            try
            {
                // 서버 허브의 SendMessage가(targetEmail, ChatMessageDto)를 받도록 맞춥니다.
                // dto 안에 이미 RoomId, SenderId가 있으니 굳이 따로 보낼 필요가 없죠!
                //await _connection.InvokeAsync("SendMessage", targetEmail, dto);

                await _connection.InvokeAsync("SendMessageDto", targetEmail, dto);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR DTO 전송 에러: {ex.Message}");
            }
        }
    }


}
