using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommonLib.Models
{
    [ObservableObject]
    public partial class Friend
    {
        [ObservableProperty]
        public string _myEmail  = string.Empty;

        [ObservableProperty]
        private string _targetEmail = string.Empty;

        [ObservableProperty]  
        private string _friendName = string.Empty;

        [ObservableProperty]
        private string _statusMsg = string.Empty;

        [ObservableProperty]
        private string? _profileImg;
    }
}
