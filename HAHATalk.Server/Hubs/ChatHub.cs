using CommonLib.Dtos;
using CommonLib.Enums;
using CommonLib.Models;
using HAHATalk.Server.Repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Org.BouncyCastle.Asn1.Mozilla;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Specialized;


namespace HAHATalk.Server.Hubs
{
    // Hub를 상속받으면 실시가 통신 서버가 됨 
    public class ChatHub : Hub
    {
        private readonly IChatRepository _chatRepository;
        // [Key: ConnectionId, Value : RoomId] ->어떤 유저가 어떤 방에 있는지 추적 
        private static readonly ConcurrentDictionary<string, string> _userRooms = new ConcurrentDictionary<string, string>();

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
                var sendTime = DateTime.Now;
                var messageGuid = Guid.NewGuid().ToString();

                // 실제 대화 내역 DB 저장 
                var chatMsg = new ChatMessage
                {
                    RoomId = roomId,
                    SenderId = senderId,
                    Message = message,
                    SendTime = sendTime,
                    MessageGuid = messageGuid,
                    MessageType = 0,        // 텍스트 
                    IsRead = false
                };

                await _chatRepository.MSSQL_SaveMessageAsync(chatMsg);


                // 채팅목록 상태 업데이트 (전광판 갱신 + 멤버 등록)
                var msgDto = new ChatMessageDto
                {
                    RoomId = roomId,
                    SenderId = senderId,
                    Message = message,
                    SendTime = sendTime,                 
                    SenderName = senderId,
                    MessageGuid = messageGuid,
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

        public async Task SendFileMessage(string roomId, string senderId, string targetId, string filePath, string fileName, int messageType)
        {
            try
            {
                var sendTime = DateTime.Now;
                var messageGuid = Guid.NewGuid().ToString();
                var type = (ChatMessageTypes)messageType;

                string previewText = type switch
                {
                    ChatMessageTypes.Image => "사진을 보냈습니다.",
                    ChatMessageTypes.Video => "동영상을 보냈습니다.",
                    ChatMessageTypes.File => $"파일 전송: {fileName}",
                    _ => "메세지가 도착했습니다."
                };

   
                // 파일 메세지 DB 저장 
                var chatMsg = new ChatMessage
                {
                    RoomId = roomId,
                    SenderId = senderId,
                    Message = previewText,
                    FilePath = filePath,
                    FileName = fileName,
                    SendTime = sendTime,
                    MessageGuid = messageGuid,
                    MessageType = (int)type, // 이미지 
                    IsRead = false,
                    SendState = (int)ChatMessage.MessageStatus.Success
                };

                await _chatRepository.MSSQL_SaveMessageAsync(chatMsg);

                // 목록 업데이트 
                var msgDto = new ChatMessageDto
                {
                    RoomId = roomId,
                    SenderId = senderId,
                    Message = previewText, // 목록 미리보기용
                    FilePath = filePath,
                    FileName = fileName,        // Dto에도 파일명을 알려줘야 클라이언트에서 다운로드 가능 
                    MessageType = (int)type, // 이미지 타입
                    SendTime = sendTime,
                    SenderName = senderId,
                    MessageGuid = messageGuid,
                };

                await _chatRepository.MSSQL_UpdateChatListAsync(msgDto, targetId, targetId, senderId, senderId);
             
                // 실시간 전송 
                await Clients.Group(roomId).SendAsync("ReceiveMessage", msgDto);
                await Clients.User(targetId).SendAsync("UpdateChatList");

            }
            catch(Exception ex)
            {
                Log.Error(ex, "[ChatHub] 파일 메세지 처리 중 오류 발생");
            }            
        }

        // 채팅방 입장 (채팅창 열 때 호출) 
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // 1. 현재 접속자 명단에 등록 (메모리)
            _userRooms[Context.ConnectionId] = roomId;

            // 2. DB에서 이 방에 등록된 "전체 멤버 수" 조회
            int totalMemberCount = await _chatRepository.MSSQL_GetRoomMemberCountAsync(roomId);

            // 만약 DB에 멤버 정보가 아직 없다면 최소 1명으로 표시
            if (totalMemberCount == 0) totalMemberCount = 1;

            // 클라이언트에게 전체 멤버 수를 보냄
            await Clients.Group(roomId).SendAsync("UpdateUserCount", totalMemberCount);

            Log.Information("[SignalR] 유저({ConnectionId}) 방({RoomId}) 입장. 전체 멤버: {Count}",
                Context.ConnectionId, roomId, totalMemberCount);
        }

        public override async Task OnConnectedAsync()
        {
            // 연결된 사용자의 식별자(EmailUserIdProvider가 설정한 값)를 가져옵니다.
            var userId = Context.UserIdentifier;

            // 🔥 서버 콘솔에 화끈하게 로그 출력!
            Log.Information("===============================================");
            Log.Information($"[접속 알림] 유저 로그인 성공: {userId}");
            Log.Information($"[접속 시간] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Log.Information("===============================================");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_userRooms.TryRemove(Context.ConnectionId, out string? roomId))
            {
                // 유저가 접속을 끊었을 때, 
                // 만약 '실시간 접속자 수'를 보여주는 UI라면 여기서 GetRoomParticipantCount(roomId)를 썼겠지만
                // '전체 멤버 수'를 보여주는 경우라면 굳이 UpdateUserCount를 다시 쏠 필요는 없습니다.
                // 다만, 인원수 동기화를 위해 다시 한번 보내주는 것이 안전합니다.
                int totalMemberCount = await _chatRepository.MSSQL_GetRoomMemberCountAsync(roomId);
                await Clients.Group(roomId).SendAsync("UpdateUserCount", totalMemberCount);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // 특정방의 인원수를 가져오는 메소드 
        private int GetRoomParticipantCount(string roomId)
        {
            return _userRooms.Values.Count(r => r == roomId);
        }

        // 채팅방 퇴장 (그룹해제) 
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            Log.Information("[SignalR] 유저({ConnectionId})가 방({RoomId})에 퇴장", Context.ConnectionId, roomId);

        }

        // 
        public async Task SendReadReceipt(string roomId, string senderId, string readerId)
        {
            try
            {
                await _chatRepository.MarkAsReadAsync(roomId, readerId);

                // 🔥 수정: Clients.User 대신 Clients.Group을 사용해 보세요.
                // 프리덤이 이 방에 접속해 있다면 무조건 신호를 받게 됩니다.
                await Clients.Group(roomId).SendAsync("ReceiveReadReceipt", roomId);

                Log.Information($"[읽음신호] {readerId}가 읽음 -> 방 전체에 알림");
            }
            catch (Exception ex) { Log.Error(ex, "에러"); }


            /*
            // senderId: 메시지를 보낸 사람 (프리덤 - '1'이 사라져야 할 사람)
            // readerId: 메시지를 읽은 사람 (기로로 - 지금 포커스를 잡은 사람)

            try
            {
                // 1. DB 업데이트: 읽은 사람(기로로)의 안 읽은 개수를 0으로, 
                //    상대방(프리덤)이 보낸 메시지를 읽음(IsRead=1) 처리
                await _chatRepository.MarkAsReadAsync(roomId, readerId);

                // 2. 메시지를 보낸 사람(senderId = 프리덤)에게 신호 전송
                // Clients.User가 안 먹힐 경우를 대비해 Group으로 쏘는 것도 방법입니다.
                await Clients.User(senderId).SendAsync("ReceiveReadReceipt", roomId);

                Log.Information($"[읽음] {readerId}가 읽음 -> {senderId}에게 알림 (Room: {roomId})");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SendReadReceipt 에러");
            }
            */
        }
    }
}
