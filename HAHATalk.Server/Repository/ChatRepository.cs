using Dapper;
using CommonLib.Models;
using CommonLib.Dtos;
using HAHATalk.Server.Repository;
using Microsoft.Data.SqlClient;
using System.Data;
using Serilog;
using HAHATalk.Server.Repository;

namespace HAHATalk.Server.Repository
{
    public class ChatRepository : RepositoryBase, IChatRepository
    {
        public ChatRepository(IConfiguration configuration) : base(configuration)
        {
            
        
                
        }

        // 채팅목록 가져오기 
        public async Task<List<ChatList>> MSSQL_GetChatListAsync(string email)
        {
            const string query = "SELECT * FROM ChatList WHERE OwnerId = @email ORDER BY LastTime DESC";
            try
            {
                using var db = CreateConnection();
                var result = await db.QueryAsync<ChatList>(query, new { email });
                return result.ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "채팅 목록 조회 중 오류 발생 (Email: {Email})", email);
                return new List<ChatList>();
            }
        }

        // 채팅방 메세지 내역 가져오기
        public async Task<List<ChatMessage>> MSSQL_GetMessageByRoomIdAsync(string roomId)
        {
            const string query = @"SELECT RoomId, SenderId, Message, SendTime, IsRead, MessageType, FilePath, FileName, MessageGuid
                                 FROM ChatMessage WHERE RoomId = @roomId ORDER BY SendTime ASC";

            try
            {
                using var db = CreateConnection();
                var result = await db.QueryAsync<ChatMessage>(query, new { roomId });
                return result.ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "메시지 내역 조회 중 오류 발생 (RoomId: {RoomId})", roomId);
                return new List<ChatMessage>();
            }
        }

        public async Task<int> MSSQL_GetTotalUnreadCountAsync(string email)
        {
            const string query = "SELECT ISNULL(SUM(UnreadCount), 0) FROM ChatList WHERE OwnerId = @email";

            try
            {
                using var db = CreateConnection();
                return await db.ExecuteScalarAsync<int>(query, new { email });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "총 안 읽은 메시지 수 조회 오류 (Email: {Email})", email);
                return 0;
            }
        }

        // 메세지 저장
        public async Task<bool> MSSQL_SaveMessageAsync(ChatMessage message)
        {   
            // MessageGuid가 있을 경우 중복 확인 후 INSERT (IF NOT EXISTS 활용) 
            // MessageGuid가 없을 경우 (과거 데이터등) 바로 INSERT

            const string query = @"
                IF @MessageGuid IS NOT NULL AND @MessageGuid <> ''
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM ChatMessage WHERE MessageGuid = @MessageGuid)
                    BEGIN
                        INSERT INTO ChatMessage (RoomId, SenderId, Message, SendTime, IsRead, MessageType, FilePath, FileName, MessageGuid) 
                        VALUES (@RoomId, @SenderId, @Message, @SendTime, @IsRead, @MessageType, @FilePath, @FileName, @MessageGuid)
                    END
                END
                ELSE
                BEGIN
                    INSERT INTO ChatMessage (RoomId, SenderId, Message, SendTime, IsRead, MessageType, FilePath, FileName, MessageGuid) 
                    VALUES (@RoomId, @SenderId, @Message, @SendTime, @IsRead, @MessageType, @FilePath, @FileName, @MessageGuid)
                END";

            try
            {
                using var db = CreateConnection();
                int rows = await db.ExecuteAsync(query, message);   // Dapper가 message 객체의 프로터피를 자동으로 mapping 

                                                                    // rows가 0인 경우는 '중복되어서 INSERT가 안 된 경우'입니다.
                                                                    // 클라이언트에게는 성공(true)을 보내야 재전송을 멈추므로 true를 반환하는 로직이 핵심입니다.
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "메시지 저장 중 오류 발생 (RoomId: {RoomId}, SenderId: {SenderId}, Guid: {Guid})",
                    message.RoomId, message.SenderId, message.MessageGuid);
                return false;
            }
        }

        // 채팅창 목록 상태 업데이트 (나와 상대방의 목록을 동시에 갱신)
        public async Task<bool> MSSQL_UpdateChatListAsync(ChatMessageDto message, string targetId, string targetName, string myId, string myNickname)
        {
            const string query = @"
                -- 1. 내(발신자) 목록 처리
                IF NOT EXISTS (SELECT 1 FROM ChatList WHERE RoomId = @roomId AND OwnerId = @myId)
                    INSERT INTO ChatList (RoomId, OwnerId, TargetId, TargetName, LastMessage, LastTime, UnreadCount, IsTop)
                    VALUES (@roomId, @myId, @targetId, @targetName, @msgText, @lastTime, 0, 0);
                ELSE
                    UPDATE ChatList 
                    SET LastMessage = @msgText, LastTime = @lastTime, TargetName = @targetName
                    WHERE RoomId = @roomId AND OwnerId = @myId;

                -- 2. 상대방(수신자) 목록 처리
                IF NOT EXISTS (SELECT 1 FROM ChatList WHERE RoomId = @roomId AND OwnerId = @targetId)
                    INSERT INTO ChatList (RoomId, OwnerId, TargetId, TargetName, LastMessage, LastTime, UnreadCount, IsTop)
                    VALUES (@roomId, @targetId, @myId, @myNickname, @msgText, @lastTime, 1, 0);
                ELSE
                    UPDATE ChatList 
                    SET LastMessage = @msgText, LastTime = @lastTime, UnreadCount = UnreadCount + 1, TargetName = @myNickname
                    WHERE RoomId = @roomId AND OwnerId = @targetId;";

            try
            {
                using var db = CreateConnection();

                // 트랜잭션을 사용하여 두 작업을 원자적으로 처리
                if (db.State != ConnectionState.Open) db.Open();
                
                using var trans = db.BeginTransaction();


                int rows = await db.ExecuteAsync(query, new
                {
                    roomId = message.RoomId,
                    myId,
                    targetId,
                    targetName,
                    myNickname,
                    msgText = message.Message,
                    lastTime = message.SendTime
                }, transaction: trans);

                trans.Commit();

                return rows > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ChatList 업데이트 중 오류 발생 (RoomId: {RoomId})", message.RoomId);
                return false;
            }
        }

        // 읽음 상태 업데이트(Dapper 통합)
        public async Task<bool> MSSQL_UpdateReadStatusAsync(string roomId, string userId)
        {
            const string query = @"
                UPDATE ChatMessage SET IsRead = 1 WHERE RoomId = @roomId AND SenderId <> @userId AND IsRead = 0;
                UPDATE ChatList SET UnreadCount = 0 WHERE RoomId = @roomId AND OwnerId = @userId;";

            try
            {
                using var db = CreateConnection();
                await db.ExecuteAsync(query, new { roomId, userId });

                Log.Information("[ReadStatus] 방({RoomId}) 유저({UserId}) 읽음 처리 완료", roomId, userId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "읽음 처리 중 오류 발생 (RoomId: {RoomId}, UserId: {UserId})", roomId, userId);
                return false;
            }
        }

        public async Task<bool> MarkAsReadAsync(string roomId, string userId)
        {
            return await MSSQL_UpdateReadStatusAsync(roomId, userId);
        }
    }
}