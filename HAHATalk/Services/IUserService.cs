using Microsoft.Xaml.Behaviors.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace HAHATalk.Services
{
    // 2026.04.01 IUserService 추가 
    public interface IUserService
    {
        string Id { get; }
        string Email { get; }
        string Name { get;  }
        bool IsLoggedIn { get;}
    }
}
