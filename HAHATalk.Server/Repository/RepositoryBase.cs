using CommonLib.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Data;
using System.Diagnostics.Contracts;

namespace HAHATalk.Server.Repository
{
    public abstract class RepositoryBase
    {
        protected  readonly IConfiguration _configuraton;
        private readonly string _connectionString;

        // 서버는 생성자를 통해 설정을 전달 받음 
        protected RepositoryBase(IConfiguration configuraton)
        {
            _configuraton = configuraton;
            _connectionString = _configuraton.GetConnectionString("MSAccountDb")
                ?? throw new InvalidOperationException("데이터베이스 연결 문자열 'MSAccountDb'가 설정 파일에 없습니다.");
        }

        protected IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
