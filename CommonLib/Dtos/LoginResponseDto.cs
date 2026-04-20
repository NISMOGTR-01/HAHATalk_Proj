using CommonLib.Models;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Dtos
{
    public class LoginResponseDto
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; } = string.Empty;

        public string? Token { get; set; }

        public Account? UserAccount { get; set; }
    }
}
