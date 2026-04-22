using Dapper;
using CommonLib.Models;
using HAHATalk.Server.Repository;
using Microsoft.Data.SqlClient;
using System.Data;
using Serilog;


using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;

namespace HAHATalk.Server.Repository
{
    public class FriendRepository : RepositoryBase, IFriendRepository
    {
        public FriendRepository(IConfiguration configuration) : base(configuration)
        {

        }
        
            
        
        // 친구추가 
        public async Task<bool> AddFriendAsync(string myId, string friendEmail, string friendName, string statusMsg)
        {
            const string query = @"
                INSERT INTO Friends (my_email, target_email, friend_name, status_msg)
                VALUES (@myId, @friendEmail, @friendName, @statusMsg)";

            try
            {
                using var db = CreateConnection();
                // 익명 객체를 통해 파라미터를 넘기면 Dapper가 알아서 매핑합니다.
                int rowsAffected = await db.ExecuteAsync(query, new { myId, friendEmail, friendName, statusMsg });

                Log.Information("[Friend] 친구 추가 완료: {MyId} -> {FriendEmail}", myId, friendEmail);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Friend] 친구 추가 중 오류 발생: {MyId} -> {FriendEmail}", myId, friendEmail);
                return false;
            }
        }

        // 친구 중복 체크 (Dapper의 ExecuteScalar 이용)
        public async Task<bool> IsFriendAlreadyExistsAsync(string myId, string friendEmail)
        {
            const string query = @"
                SELECT COUNT(1)
                FROM Friends 
                WHERE my_email = @myId AND target_email = @friendEmail";

            try
            {
                using var db = CreateConnection();
                // 결과를 단일 값으로 받아올 때는 ExecuteScalarAsync가 가장 효율적입니다.
                int count = await db.ExecuteScalarAsync<int>(query, new { myId, friendEmail });
                return count > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Friend] 친구 중복 체크 중 오류 발생: {MyId}, {FriendEmail}", myId, friendEmail);
                return false;
            }
        }

        // MSSQL 서버에서 친구를 가져오는 메소드 
        public async Task<List<Friend>> MSSQL_GetFriendsAsync(string myEmail)
        {
            // 쿼리 결과의 컬럼명과 Friend 클래스의 프로퍼티명이 다를 경우 AS를 사용하거나 
            // Dapper의 매핑 설정을 활용할 수 있습니다. 
            // Friend 모델의 프로퍼티가 MyEmail, TargetEmail 형태라면 아래처럼 조회합니다.
            const string query = @"
                SELECT 
                    my_email AS MyEmail, 
                    target_email AS TargetEmail, 
                    friend_name AS FriendName, 
                    status_msg AS StatusMsg
                FROM Friends
                WHERE my_email = @myEmail";

            try
            {
                using var db = CreateConnection();
                // QueryAsync<T> 한 줄로 모든 리스트 매핑이 끝납니다.
                var friends = await db.QueryAsync<Friend>(query, new { myEmail });

                return friends.ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Friend] 친구 목록 조회 중 오류 발생: {MyEmail}", myEmail);
                return new List<Friend>();
            }
        }
    }   

}