using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HAHATalk.Server.Models
{
    [Table("account")]      // 2026.03.26 MSSQL DB의 실제 테이블 이름과 맞추어야 
    public class Account
    {
        [Key]   // PK 기본키 설정 
        [StringLength(50)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string Pwd { get; set; }

        [StringLength(20)]
        public string? Nickname { get; set; }

        [StringLength(11)]
        public string? Cell_phone { get; set; }

        public DateTime Create_date { get; set; } = DateTime.Now; // 현재 시간으로 기본설정 

        // ADD
        [NotMapped]
        public string? RawPassword { get; set; }

        /// <summary>
        /// 2026.05.16 Add
        /// 잠금모드에 사용되는 BCrypt 암호화된 60글자 비밀번호 문자열
        /// DB의 lock_pwd 컬럼과 매칭.
        /// </summary>
        [StringLength(64)] // 스키마의 nvarchar(64) 크기와 일치하도록 설정
        public string? LockPassword { get; set; }
    }
}
