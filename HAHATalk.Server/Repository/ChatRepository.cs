using CommonLib.DataBase;
using CommonLib.Models;
using HAHATalk.Server.Data;
using CommonLib.Dtos;
using HAHATalk.Server.Repository;
using Microsoft.Data.SqlClient; // [필수] SqlDataReader와 ReadAsync를 위해 필요
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;


namespace HAHATalk.Server.Repositories
{
    public class ChatRepository : RepositoryBase, IChatRepository
    {
        // DB 컨텍스트 변수 
        private readonly AppDbContext _context;

        public ChatRepository(IConfiguration configuration) : base(configuration)
        {
            // 서버 환경에서는 생성자를 통해 설정값을 전달받아
            // 부모 클래스로 (RepositoryBase)로 전달 
        }

        // 채팅목록 가져오기 (비동기)        
        public async Task<List<ChatList>> MSSQL_GetChatListAsync(string email)
        {
            string query = "SELECT * FROM ChatList WHERE OwnerId = @email ORDER BY LastTime DESC";
            
            var list = new List<ChatList>();

            using (MSSqlDb db = MSAccountDb)
            {
                // MSSqlDb -> SqlDataReader return 
                using (SqlDataReader dr = await db.GetReaderAsync(query, new SqlParameter[]
                {
                    new SqlParameter("email", email)
                }))
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new ChatList
                        {
                            RoomId = dr["RoomId"]?.ToString()!,
                            TargetId = dr["TargetId"]?.ToString()!, // [추가!!!] : 2026-04-13 TargetId 누락 보정
                            TargetName = dr["TargetName"]?.ToString()!,
                            LastMessage = dr["LastMessage"]?.ToString()!,
                            LastTime = dr["LastTime"] != DBNull.Value ? Convert.ToDateTime(dr["LastTime"]) : DateTime.Now,
                            UnreadCount = dr["UnreadCount"] != DBNull.Value ? Convert.ToInt32(dr["UnreadCount"]) : 0,
                            ProfileImg = dr["ProfileImg"]?.ToString()!,
                        });
                    }
                }
            }
            return list;

        }

        // 채팅방 메세지 내역 가져오기 
        public async Task<List<ChatMessage>> MSSQL_GetMessageByRoomIdAsync(string roomId)
        {
            string query = "SELECT RoomId, SenderId, Message, SendTime, IsRead, MessageType, FilePath, FileName  FROM ChatMessage WHERE RoomId = @roomId ORDER BY SendTime ASC";

            var list = new List<ChatMessage>();
            
            using (MSSqlDb db = MSAccountDb)
            {
                using (SqlDataReader dr = await db.GetReaderAsync(query, new[] {new SqlParameter("roomId", roomId)}))
                {
                    while(await dr.ReadAsync())
                    {
                        list.Add(new ChatMessage
                        {
                            RoomId = dr["RoomId"].ToString()!,
                            SenderId = dr["SenderId"].ToString()!,
                            Message = dr["Message"].ToString()!,
                            SendTime = Convert.ToDateTime(dr["SendTime"]),
                            MessageType = dr["MessageType"] != DBNull.Value ? Convert.ToInt32(dr["MessageType"]) : 0,
                            FilePath = dr["FilePath"]?.ToString() ?? string.Empty,
                            FileName = dr["FileName"]?.ToString() ?? string.Empty,
                            IsRead = dr["IsRead"] != DBNull.Value && Convert.ToBoolean(dr["IsRead"]),
                            IsMine = false
                        });
                    }
                }
            }

            return list;
        }
        
        // 안 읽은 메세지 총합 
        public async Task<int> MSSQL_GetTotalUnreadCountAsync(string email)
        {
            // 모든 채팅방의 안 읽은 메세지 총합(비동기) 
            string query = "SELECT ISNULL(SUM(UnreadCount), 0) FROM ChatList WHERE OwnerId = @email";

            // 비동기로 실행하기 위해 Task.Run을 활용 또는 
            // 만약 db.GetReaderAsync가 없다면 아래와 같이 구성
          
            using(MSSqlDb db = MSAccountDb)
            {
                using(SqlDataReader dr = await db.GetReaderAsync(query, new SqlParameter[] {new SqlParameter ("email", email) }))
                {
                    if(await dr.ReadAsync())
                    {
                        return Convert.ToInt32(dr[0]);
                    }
                }
            }

            return 0;
            
        }

        // 2026.04.01 메세지 저장 
        public async Task<bool> MSSQL_SaveMessageAsync(ChatMessage message)
        {
            // MessageId는 PK(bigint)이므로 보통 자동증가(Identity)일 확률이 높으니 제외하고 insert 합니다.
            string query = @"INSERT INTO ChatMessage (RoomId, SenderId, Message, SendTime, IsRead, MessageType, FilePath, FileName) 
                     VALUES (@roomId, @senderId, @message, @sendTime, @isRead, @messageType, @filePath, @fileName)";

            return await Task.Run(() =>
            {
                using (MSSqlDb db = MSAccountDb)
                {
                    SqlParameter[] @params = new SqlParameter[]
                    {
                        new SqlParameter("@roomId", message.RoomId),
                        new SqlParameter("@senderId", message.SenderId),
                        new SqlParameter("@message", message.Message),
                        new SqlParameter("@sendTime", message.SendTime),
                        new SqlParameter("@isRead", message.IsRead),
                        new SqlParameter("@messageType", message.MessageType),
                        new SqlParameter("@filePath", (object)message.FilePath ?? DBNull.Value),        // 2026.04.07 필드 추가 (filePath, fileName) 
                        new SqlParameter("@fileName", (object)message.FileName ?? DBNull.Value)
                    };

                    double result = db.Execute(query, @params);
                    return result > 0;
                }
            });
        }
        // 2026.04.01 채팅창 업데이트 
        public async Task<bool> MSSQL_UpdateChatListAsync(ChatMessageDto message, string targetId, string targetName, string myId, string myNickname)
        {
            // 쿼리 포인트:
            // 1. 내(OwnerId = SenderId) 목록: 안 읽은 갯수 0 유지 
            // 2. 상대방(OwnerId = TargetId) 목록: 안 읽은 갯수 +1 증가!
            string query = @"
              -- 1. 내(발신자) 목록 처리
                    IF NOT EXISTS (SELECT 1 FROM ChatList WHERE RoomId = @roomId AND OwnerId = @myId)
                        INSERT INTO ChatList (RoomId, OwnerId, TargetId, TargetName, LastMessage, LastTime, UnreadCount, IsTop)
                        VALUES (@roomId, @myId, @targetId, @targetName, @message, @lastTime, 0, 0);
                    ELSE
                        UPDATE ChatList 
                        SET LastMessage = @message, LastTime = @lastTime, TargetName = @targetName
                        WHERE RoomId = @roomId AND OwnerId = @myId;

                    -- 2. 상대방(수신자) 목록 처리
                    IF NOT EXISTS (SELECT 1 FROM ChatList WHERE RoomId = @roomId AND OwnerId = @targetId)
                        INSERT INTO ChatList (RoomId, OwnerId, TargetId, TargetName, LastMessage, LastTime, UnreadCount, IsTop)
                        VALUES (@roomId, @targetId, @myId, @myNickname, @message, @lastTime, 1, 0);
                    ELSE
                        UPDATE ChatList 
                        SET LastMessage = @message, LastTime = @lastTime, UnreadCount = UnreadCount + 1, TargetName = @myNickname
                        WHERE RoomId = @roomId AND OwnerId = @targetId;
            ";

      
            using (MSSqlDb db = MSAccountDb)
            {
                double result = await db.ExecuteAsync(query, new SqlParameter[]
                {
                    new SqlParameter("@roomId", message.RoomId),
                    new SqlParameter("@myId", myId),
                    new SqlParameter("@targetId", targetId),
                    new SqlParameter("@targetName", targetName),
                    new SqlParameter("@myNickname", myNickname),
                    new SqlParameter("@message", message.Message),
                    new SqlParameter("@lastTime", message.SendTime)
                });

                return result > 0;
            }
            
        }
    }
}
