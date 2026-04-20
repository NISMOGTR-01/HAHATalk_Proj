using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HAHATalk.Controls
{
    /// <summary>
    /// FiledListControl.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
    [ObservableObject]
    public partial class FriendListControl : UserControl
    {
        
        public FriendListControl()
        {
            InitializeComponent();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            //BtnSettings.ContextMenu.IsOpen = true;       // 왼쪽 클릭 시 메뉴 강제 오픈 
        }
    }
}
