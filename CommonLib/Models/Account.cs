using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Models
{
    public class Account
    {
        //public string Id { get; set; } = default!;
        public string Email { get; set; } = string.Empty;
        public string? Nickname { get; set; } 
        public string? CellPhone { get; set; }
        public string Pwd { get; set; } = string.Empty;
    }
}
