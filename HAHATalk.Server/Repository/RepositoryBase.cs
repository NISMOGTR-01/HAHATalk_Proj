using CommonLib.DataBase;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Diagnostics.Contracts;

namespace HAHATalk.Server.Repository
{
    public abstract class RepositoryBase
    {
        protected readonly IConfiguration _configuraton;

        // 서버는 생성자를 통해 설정을 전달 받음 
        protected RepositoryBase(IConfiguration configuraton)
        {
            _configuraton = configuraton;
        }

        protected MSSqlDb MSAccountDb
        {
            get
            {
                // appsettings.json에 등록된 연결 문자열을 가져온다.
                string connectionString = _configuraton.GetConnectionString("MSAccountDb")
                    ?? throw new InvalidOperationException("데이터베이스 연결 문자열 'MSAccountDb'가 설정 파일에 없습니다.");

                return new MSSqlDb(connectionString);
            }
        }   
    }
}
