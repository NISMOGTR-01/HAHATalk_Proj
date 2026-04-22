using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HAHATalk.Server.Models
{
    [Table("ChatMessage")] // MSSQL 테이블 이름과 일치 
    public class ChatMessage
    {
        // 복합키 구성요소 (AppDbContext에서 설정) 
        [Required]
        [StringLength(50)]
        public string RoomId { get; set; }

        [Required]
        [StringLength(50)]
        public string SenderId { get; set; }

        [Required]
        public DateTime SendTime { get; set; }

        // 2. 메세지 본문 
        [Required]
        public string Message { get; set; }

        // 상태 및 타입 (MSSQL 기본값 0 설정과 매칭) 
        public bool? IsRead { get; set; } = false;

        public int? MessageType { get; set; } = 0;

        // 파일 관련 
        public string? FilePath { get; set; }

        [StringLength(255)]
        public string? FileName { get; set; }
    }
}
