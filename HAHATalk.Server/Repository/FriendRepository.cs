using CommonLib.DataBase;
using CommonLib.Models;
using HAHATalk.Server.Repository;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;


using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;

namespace HAHATalk.Server.Repositories
{
    public class FriendRepository : RepositoryBase, IFriendRepository
    {
        public FriendRepository(IConfiguration configuration) : base(configuration)
        {

        }
        
            
        

        public async Task<bool> AddFriendAsync(string myId, string friendEmail, string friendName, string statusMsg)
        {
            string query = @"
                INSERT INTO Friends (my_email, target_email, friend_name, status_msg)
                VALUES (@my_email, @target_email, @friend_name, @status_msg)";

            try
            {
                using (MSSqlDb db = MSAccountDb)
                {
                    // 
                    long rowsAffected = await db.ExecuteAsync(query, new SqlParameter[]
                    {
                        new SqlParameter("@my_email", myId),
                        new SqlParameter("@target_email", friendEmail),
                        new SqlParameter("@friend_name", friendName),
                        new SqlParameter("@status_msg", statusMsg),
                    });

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddFriendAsync Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsFriendAlreadyExistsAsync(string myId, string friendEmail)
        {
            string query = @"
                SELECT COUNT(*)
                FROM Friends 
                WHERE my_email = @my_email AND target_email = @target_email";

            try
            {
                using (MSSqlDb db = MSAccountDb)
                {
                    using (SqlDataReader dr = await db.GetReaderAsync(query, new SqlParameter[]
                    {
                        new SqlParameter("my_email", myId),
                        new SqlParameter("target_email", friendEmail)
                    }))
                    {
                        // 데이터가 있으면 읽고 그 값이 0보다 큰지 확인 (1이상이면 계정 존재) 
                        if(await dr.ReadAsync())
                        {
                            int count = dr.GetInt32(0); // 첫 번째 컬럼을 가져옴 
                            return count > 0; 
                        }

                        return false;

                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        // MSSQL 서버에서 친구를 가져오는 메소드 
        public async Task<List<Friend>> MSSQL_GetFriendsAsync(string myEmail)
        {
            List<Friend> list = new List<Friend>();

            // friend Table 쿼리 조회 
            string query = @"
                SELECT my_email, target_email, friend_name, status_msg
                FROM Friends
                WHERE my_email = @my_email";

            // RepositoryBase에서 상속받는 MSAccountDb 사용 
            using (MSSqlDb db = MSAccountDb)
            {
                using (var dr = await db.GetReaderAsync(query, new SqlParameter[]
                {
                    new SqlParameter("@my_email", myEmail)
                }))
                {
                    while (await dr.ReadAsync())
                    {
                        list.Add(new Friend
                        {
                            MyEmail = dr["my_email"].ToString()!,
                            TargetEmail = dr["target_email"].ToString()!,
                            FriendName = dr["friend_name"].ToString()!,
                            StatusMsg = dr["status_msg"].ToString()!,
                        });
                    }
                }
            }

            return list;
        }



    }   

}