using System;
using System.Collections.Generic;
using System.Text;

namespace HAHATalk.Services
{
    public interface IWindowManager
    {
        // 이름(ID)를 받아서 해당 채팅창 띄어주는 역할 
        void ShowChatRoom(string roomId, string targetId, string targetName, string targetProfile);

        // (옵션) 모든 창 닫기 등 확장기능 
        void CloseAll();
    }
}
