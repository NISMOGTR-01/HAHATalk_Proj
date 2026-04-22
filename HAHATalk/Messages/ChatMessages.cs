using CommonLib.Dtos;

namespace HAHATalk.Messages
{
    // 서버에서 신규 메시지가 도착했을 때 사용하는 메신저 티켓 
    public record NewMessageReceivedMessage(ChatMessageDto Message);

    // 상대방이 메시지를 읽었다는 신호를 받았을 때 사용하는 메신저 티켓 
    public record MessagesReadMessage(string RoomId);

    // 채팅 목록 화면을 갱신해야 할 때 사용하는 메신저 티켓 
    public record RefreshChatListMessage();
}
