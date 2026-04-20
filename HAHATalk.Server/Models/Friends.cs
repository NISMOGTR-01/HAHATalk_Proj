using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HAHATalk.Server.Models
{
    [Table("friends")]
    public class Friends
    {
        // AppDbContext에서 복합키(my_email + target_email)로 설정할 예정
        [StringLength(50)]
        public string my_email { get; set; }

        [StringLength(50)]
        public string target_email { get; set; }

        [StringLength(50)]
        public string friend_name { get; set; }

        [StringLength(200)]
        public string status_msg { get; set; }
    }
}
