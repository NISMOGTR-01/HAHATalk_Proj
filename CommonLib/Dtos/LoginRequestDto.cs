using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Dtos
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Pwd {  get; set; } = string.Empty;
    }
}
