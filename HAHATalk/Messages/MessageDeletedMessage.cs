using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HAHATalk.Messages
{
    /// <summary>
    /// 상대방이 메시지를 실시간으로 삭제했을 때 발송되는 메신저 신호 DTO
    /// </summary>
    public class MessageDeletedMessage
    {
        public string RoomId { get; }
        public string MessageGuid { get; }

        public MessageDeletedMessage(string roomId, string messageGuid)
        {
            RoomId = roomId;
            MessageGuid = messageGuid;
        }
    }
}