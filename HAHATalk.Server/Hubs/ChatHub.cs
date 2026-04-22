using CommonLib.Dtos;

using HAHATalk.Server.Repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Mozilla;
using Serilog;


namespace HAHATalk.Server.Hubs
{
    // Hub를 상속받으면 실시가 통신 서버가 됨 
    public class ChatHub : Hub
    {
        private readonly IChatRepository _chatRepository;

        // 클라이언트에서 'SendMessage'라고 호출하면 이 메서드가 실행 

        // 생성자 주입으로 DB 컨텍스트 가져오기 
        
        public ChatHub(IChatRepository repository)
        {
            _chatRepository = repository;
        }


        // 클라이언트 에서 'SendMessage'를 호출할 때 
        public async Task SendMessage(string roomId, string senderId, string targetId, 
            string message)
        {
            Log.Information("[SignalR] 메시지 수신: {SenderId} -> {TargetId} (Room: {RoomId})", senderId, targetId, roomId);
            try
            {
                // DB 연동 (Dapper Repository) 
                var msgDto = new ChatMessageDto
                {
                    RoomId = roomId,
                    SenderId = senderId,
                    Message = message,
                    SendTime = DateTime.Now,
                  
                    SenderName = senderId
                };

                // DB 업데이트 
                await _chatRepository.MSSQL_UpdateChatListAsync(
                                    msgDto,
                                    targetId,
                                    targetId,
                                    senderId,
                                    senderId
                );

                // 방에 있는 사람들에게 전송 (현재 채팅창 오픈 멤버)
                // 전송 데이터 형식을 DTO 객체 하나로 통일
                await Clients.Group(roomId).SendAsync("ReceiveMessage", msgDto);

                // 상대방 개인에게도 전송 (상대방이 목록 화면에 있을 때 갱신용) 
                // 상대방이 UpdateChatList 신호를 받으면 목록을 새로고침
                await Clients.User(targetId).SendAsync("UpdateChatList");
            }
            catch(Exception ex)
            {
                Log.Error(ex, "[ChatHub] 메시지 처리 중 오류 발생 (Room: {RoomId})", roomId);
            }
        }

        // 채팅방 입장 (채팅창 열 때 호출) 
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            Log.Information("[SignalR] 유저({ConnectionId})가 방({RoomId})에 입장", Context.ConnectionId, roomId);
        }

        // 채팅방 퇴장 (그룹해제) 
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            Log.Information("[SignalR] 유저({ConnectionId})가 방({RoomId})에 퇴장", Context.ConnectionId, roomId);

        }

        // 
        public async Task SendReadReceipt(string roomId, string targetId)
        {
            // 메세지를 보낸 사람(targetId)에게 "상대방이 읽음"신호를 보냄 
            // tagetId : 메세지 발신자 id 
            await Clients.User(targetId).SendAsync("ReceiveReadReceipt", roomId);
        }
    }
}
