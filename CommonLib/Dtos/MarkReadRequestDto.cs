using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Dtos
{
    public class MarkReadRequestDto
    {
        public string RoomId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
