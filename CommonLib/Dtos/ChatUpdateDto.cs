using CommonLib.Models;

namespace CommonLib.Dtos
{
    public class ChatUpdateDto
    {
        public ChatMessageDto Message { get; set; } = null!;

        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string MyId { get; set; } = string.Empty;
        public string MyNickname { get; set; } = string.Empty;
    }
}