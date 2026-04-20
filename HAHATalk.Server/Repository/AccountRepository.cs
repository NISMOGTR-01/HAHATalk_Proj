using CommonLib.DataBase;
using CommonLib.Models;
using HAHATalk.Server.Repository;
using Microsoft.Data.SqlClient; // [필수]
using Org.BouncyCastle.Bcpg.OpenPgp;
using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;
using HAHATalk.Server.Security;


namespace HAHATalk.Server.Repositories
{
    public class AccountRepository : RepositoryBase, IAccountRepository
    {
        // 부모인 RepositoryBase에 IConfi
        public AccountRepository(IConfiguration configuration) : base(configuration) 
        {
        
        }
        
        public async Task<bool> MSSQL_ExistEmailAsync(string email)
        {
            string query = "SELECT 1 FROM account WHERE email = @email";

            using (MSSqlDb db = MSAccountDb)
            {
                using (var dr = await db.GetReaderAsync(query, new SqlParameter[] {
                    new SqlParameter("@email", email)
                }))
                {
                    return await dr.ReadAsync();
                }
            }
        }

        public async Task<string?> MSSQL_Find_AccountAsync(Account account)
        {
            string phoneNumber = account.CellPhone?.Replace("-", "") ?? "";
            string query = "SELECT email FROM account WHERE cell_phone = @cell_phone";

            using (MSSqlDb db = MSAccountDb)
            {
                using (var dr = await db.GetReaderAsync(query, new SqlParameter[]
                {
                    new SqlParameter("@cell_phone", phoneNumber)
                }))
                {
                    if(await dr.ReadAsync())
                    {
                        return dr["email"].ToString();
                    }
                }
            }

            return "0";
        }

        public async Task<Account?> MSSQL_GetAccountByEmailAsync(string email)
        {
            string query = "SELECT email, nickname, pwd FROM account WHERE email = @email";

            using (MSSqlDb db = MSAccountDb)
            {
                using (var dr = await db.GetReaderAsync(query, new SqlParameter[]
                {
                    new SqlParameter("email", email)
                }))
                {
                    if(await dr.ReadAsync())
                    {
                        return new Account
                        {
                            Email = dr["email"].ToString()!, 
                            Nickname = dr["nickname"].ToString()!,
                            Pwd = dr["pwd"].ToString()! // 
                        };
                    }
                }                
            }
            return null;
        }

        public async Task<bool> MSSQL_Login_CheckAsync(string email, string pwd)
        {
            var account = await MSSQL_GetAccountByEmailAsync(email);

            if(account == null)
            {
                return false;
            }

            return SecurityHelper.VerifyPassword(pwd, account.Pwd);
        }

        public async Task<long> MSSQL_Pass_UpdateAsync(Account account, string changePwd)
        {
            // 변경할 비밀번호 암호화
            string hashedPwd = SecurityHelper.HashPassword(changePwd);

            string query = "UPDATE account SET pwd = @pwd WHERE email = @email";

            using (MSSqlDb db = MSAccountDb)
            {
                return await db.ExecuteAsync(query, new SqlParameter[] 
                { 
                    new SqlParameter("@pwd", hashedPwd),
                    new SqlParameter("@email", account.Email),

                });
            }
        }

        public async Task<long> MSSQL_SaveAsync(Account account, string changPwd)
        {
            // 회원 가입시 비밀번호 암호화 
            // parameter로 받은 changePwd가 있다면 그것을,없다면 account.Pwd를 암호화 
            string hashedPwd = SecurityHelper.HashPassword(!string.IsNullOrEmpty(changPwd) ? changPwd : account.Pwd);


            string query = @"
                INSERT INTO account (pwd, email, nickname, cell_phone)
                VALUES (@pwd, @email, @nickname, @cell_phone);";

            using (MSSqlDb db = MSAccountDb)
            {
                return await db.ExecuteAsync(query, new SqlParameter[]
                {
                    new SqlParameter("@pwd", hashedPwd),
                    new SqlParameter("@email", account.Email),
                    new SqlParameter("@nickname", account.Nickname),
                    new SqlParameter("@cell_phone", account.CellPhone),
                });
            }


        }
    }
}
