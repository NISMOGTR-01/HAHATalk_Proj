using HAHATalk.Server.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace HAHATalk.Server.Hubs
{
    // Hub를 상속받으면 실시가 통신 서버가 됨 
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        // 클라이언트에서 'SendMessage'라고 호출하면 이 메서드가 실행 

        // 생성자 주입으로 DB 컨텍스트 가져오기 
        
        public ChatHub(AppDbContext context)
        {
            _context = context;
        }


        // 클라이언트 에서 'SendMessage'를 호출할 때 
        public async Task SendMessage(string roomId, string senderId, string targetId, 
            string message)
        {
            // DB 연동 : 채팅방 정보 업데이트 (보낸사람, 받는 사람 모두 갱신) 
            // 복합키 기준으로 찾기 (RoomId + OwnerId) 기준
            var rooms =  await _context.ChatLists
                .Where(c => c.RoomId == roomId && (c.OwnerId == senderId || c.OwnerId == targetId))
                .ToListAsync();

            foreach (var room in rooms)
            {
                room.LastMessage = message;     // 최신 메세지를 전송한 메세지로 갱신
                room.LastTime = DateTime.Now;   // 최신 시간을 현재시간으로 갱신 

                // 받는 사람의 안 읽은 메시지 수 증가 
                if(room.OwnerId == targetId)
                {
                    room.UnreadCount = (room.UnreadCount ?? 0) + 1;
                }
            }

            // DB 저장 (AccountController) 
            await _context.SaveChangesAsync();

            // 실시간 전송 : 해당 채팅방(ChatRoom)에 접속한 사람들에게만 BroadCasting 
            await Clients.Group(roomId).SendAsync("ReceiveMessage", senderId, message);
        }

        // 채팅방 입장 (채팅창 열 때 호출) 
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }
    }
}
