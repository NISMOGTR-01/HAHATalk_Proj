using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HAHATalk.Server.Models
{
    [Table("ChatList")]
    public class ChatList
    {
        // 
        [StringLength(50)]
        public string RoomId { get; set; }

        [StringLength(50)]
        public string OwnerId { get; set; }

        [StringLength(50)]
        public string TargetId { get; set; }

        [StringLength(100)]
        public string TargetName { get; set; }

        public string LastMessage { get; set; }

        public DateTime? LastTime { get; set; }

        public int? UnreadCount {  get; set; }


    }
}
