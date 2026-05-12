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
            // [수정] ChatList(c)를 기준으로 Account(a)에서 최신 프로필 이미지를, 
            // ChatMember에서 현재 방의 총 인원수를 가져옵니다.
            const string query = @"
                SELECT 
                    c.RoomId, 
                    c.OwnerId, 
                    c.TargetId, 
                    c.TargetName, 
                    c.LastMessage, 
                    c.LastTime, 
                    c.UnreadCount, 
                    c.IsTop,
                    a.profile_img AS ProfileImg, -- Account 테이블의 실시간 이미지
                    (SELECT COUNT(*) FROM ChatMember cm WHERE cm.RoomId = c.RoomId) AS ParticipantCount -- 실시간 인원수
                FROM ChatList c
                LEFT JOIN Account a ON c.TargetId = a.email
                WHERE c.OwnerId = @email 
                ORDER BY c.LastTime DESC";

            try
            {
                using var db = CreateConnection();
                // Dapper가 확장된 프로퍼티(ParticipantCount 등)도 모델에 자동으로 매핑합니다.
                var result = await db.QueryAsync<ChatList>(query, new { email });
                return result.ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "채팅 목록 조회 중 오류 발생 (Email: {Email})", email);
                return new List<ChatList>();
            }

            /*
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
            */
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
                -- 0. ChatMember 등록 (동시성 고려)
                IF NOT EXISTS (SELECT 1 FROM ChatMember WHERE RoomId = @roomId AND UserId = @myId)
                    INSERT INTO ChatMember (RoomId, UserId, JoinTime) VALUES (@roomId, @myId, GETDATE());
                IF NOT EXISTS (SELECT 1 FROM ChatMember WHERE RoomId = @roomId AND UserId = @targetId)
                    INSERT INTO ChatMember (RoomId, UserId, JoinTime) VALUES (@roomId, @targetId, GETDATE());

                -- 닉네임 확보 (NULL 방지 보강)
                DECLARE @RealTargetName NVARCHAR(100) = (SELECT TOP 1 nickname FROM Account WHERE email = @targetId);
                DECLARE @RealMyNickname NVARCHAR(100) = (SELECT TOP 1 nickname FROM Account WHERE email = @myId);

                SET @RealTargetName = ISNULL(ISNULL(@RealTargetName, @targetName), 'Unknown');
                SET @RealMyNickname = ISNULL(ISNULL(@RealMyNickname, @myNickname), 'Unknown');

                -- 상대방 안 읽은 메시지 수
                DECLARE @CurrentCount INT = (SELECT COUNT(*) FROM ChatMessage 
                                             WHERE RoomId = @roomId AND SenderId = @myId AND IsRead = 0);

                -- 1. 내 목록 처리
                IF NOT EXISTS (SELECT 1 FROM ChatList WHERE RoomId = @roomId AND OwnerId = @myId)
                    INSERT INTO ChatList (RoomId, OwnerId, TargetId, TargetName, LastMessage, LastTime, UnreadCount, IsTop, MessageType)
                    VALUES (@roomId, @myId, @targetId, @RealTargetName, @msgText, @lastTime, 0, 0, @messageType);
                ELSE
                    UPDATE ChatList 
                    SET LastMessage = @msgText, LastTime = @lastTime, TargetName = @RealTargetName, MessageType = @messageType
                    WHERE RoomId = @roomId AND OwnerId = @myId;
                
                -- 2. 상대방 목록 처리
                IF NOT EXISTS (SELECT 1 FROM ChatList WHERE RoomId = @roomId AND OwnerId = @targetId)
                    INSERT INTO ChatList (RoomId, OwnerId, TargetId, TargetName, LastMessage, LastTime, UnreadCount, IsTop, MessageType)
                    VALUES (@roomId, @targetId, @myId, @RealMyNickname, @msgText, @lastTime, @CurrentCount, 0, @messageType);
                ELSE
                    UPDATE ChatList 
                    SET LastMessage = @msgText, LastTime = @lastTime, UnreadCount = @CurrentCount, TargetName = @RealMyNickname, MessageType = @messageType
                    WHERE RoomId = @roomId AND OwnerId = @targetId;";

            // CreateConnection() 결과물을 DbConnection으로 캐스팅하여 비동기 메서드 활성화
            using var db = CreateConnection() as System.Data.Common.DbConnection;

            if (db == null)
            {
                Log.Error("[Chat] 연결 객체를 DbConnection으로 변환할 수 없습니다.");
                return false;
            }

            try
            {
                // 1. 비동기 연결 오픈
                await db.OpenAsync();

                // 2. 비동기 트랜잭션 시작
                using var trans = await db.BeginTransactionAsync();

                try
                {
                    int rows = await db.ExecuteAsync(query, new
                    {
                        roomId = message.RoomId,
                        myId = myId,
                        targetId = targetId,
                        targetName = targetName ?? "Unknown", // NULL 방어
                        myNickname = myNickname ?? "Unknown", // NULL 방어
                        msgText = message.Message ?? "",
                        lastTime = message.SendTime,
                        messageType = message.MessageType
                    }, transaction: trans);

                    // 3. 성공 시 커밋
                    await trans.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    // 4. 실패 시 롤백
                    await trans.RollbackAsync();
                    Log.Error(ex, "[Chat] ChatList 업데이트 중 오류 발생하여 롤백 처리 (RoomId: {RoomId})", message.RoomId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Chat] DB 연결 또는 트랜잭션 시작 실패 (RoomId: {RoomId})", message.RoomId);
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
            const string query = @"
                UPDATE ChatMessage SET IsRead = 1 WHERE RoomId = @roomId AND SenderId <> @userId AND IsRead = 0;
                UPDATE ChatList SET UnreadCount = 0 WHERE RoomId = @roomId AND OwnerId = @userId;";

            try
            {
                using var db = CreateConnection();
                await db.ExecuteAsync(query, new { roomId, userId });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "읽음 처리 중 오류 발생 (RoomId: {RoomId}, UserId: {UserId})", roomId, userId);
                return false;
            }
            // return await MSSQL_UpdateReadStatusAsync(roomId, userId);
        }

        // 채팅방에 인원수를 가져오는 쿼리 
        public async Task<int> MSSQL_GetRoomMemberCountAsync(string roomId)
        {
            string query = "SELECT COUNT(*) FROM ChatMember WHERE RoomId = @RoomId";

            try
            {
                using var db = CreateConnection();

                // ExecuteScalar는 결과의 첫번째 행의 첫번째 칼럼 return 
                return await db.ExecuteScalarAsync<int>(query, new { roomId });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "방 인원 수 조회 오류 (RoomId: {RoomId}", roomId);
                return 0;
            }
        }

        // 채팅방에 멤버 추가 기능 메서드 
        public async Task<bool> MSSQL_AddChatMemberAsync(string roomId, string userId)
        {
            const string query = @"
                IF NOT EXISTS (SELECT 1 FROM ChatMember WHERE RoomId = @roomId AND UserId = @userId)
                BEGIN 
                    INSERT INTO ChatMember (RoomId, UserId, JoinTime)
                    VALUES (@roomId, @userId, GETDATE())
                END";

            try
            {
                using var db = CreateConnection();
                await db.ExecuteAsync(query, new { roomId, userId });
                return true;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "멤버 초대 중 오류 발생 (RoomId: {RoomId}, UserId: {UserId})", roomId, userId);
                return false;
            }
        }
    }
}