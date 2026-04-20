using Microsoft.EntityFrameworkCore;
using HAHATalk.Server.Models;

namespace HAHATalk.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Account, Chat, Friends 
        public DbSet<Account> Accounts { get; set; }
        public DbSet<ChatList> ChatLists { get; set; }
        public DbSet<Friends> Friends { get; set; }

        // 테이블의 세부규칙을 설정하는 메서드 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 보통 테이블은 PK(기본키)가 하나여야 하는데,
            // 친구 목록이나 채팅방은 두 개의 컬럼을 합쳐서 하나의 키로 설정 

            // friends 테이블 : 복합키 설정 (my_email + target_email) 
            modelBuilder.Entity<Friends>()
                .HasKey(f => new { f.my_email, f.target_email });

            // ChatList 테이블 : 복합키 설정 (RoomId + OwnerId)
            modelBuilder.Entity<ChatList>()
                .HasKey(c => new { c.RoomId, c.OwnerId });

            // 테이블 이름이 소문자 / 대소문자 구분이 필요한 경우 명시 
            modelBuilder.Entity<Account>().ToTable("account");
            modelBuilder.Entity<Friends>().ToTable("friends");
            modelBuilder.Entity<ChatList>().ToTable("ChatList");
        }
    }
}
