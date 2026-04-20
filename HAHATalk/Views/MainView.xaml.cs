using CommunityToolkit.Mvvm.Input;
using HAHATalk.ViewModels;
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
using System.Windows.Shapes;

namespace HAHATalk.Views
{
    /// <summary>
    /// MainView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            // 생성자 주입 
            //DataContext = App.Current.Services.GetService(typeof(MainViewModel));
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            // 2026.04.05 버튼을 클릭하여 컨텍스트 메뉴 열기 
            // UI 이벤트 : 메뉴를 시각적으로 열어주는 역할만 수행 
            var btn = sender as Button;
            if (btn != null && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}
