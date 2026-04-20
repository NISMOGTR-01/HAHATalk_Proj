using System;
using System.Collections.Generic;
using System.Text;

namespace HAHATalk.Services
{
    public class TestService : ITestService
    {
        public string GetString()
        {
            return "하하톡입니다.";
        }
    }
}
