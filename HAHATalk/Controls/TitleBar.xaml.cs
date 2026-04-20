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
using WPFLib.Extensions;

namespace HAHATalk.Controls
{
    /// <summary>
    /// TitleBar.xaml에 대한 상호 작용 논리
    /// </summary>
    [ObservableObject]
    public partial class TitleBar : UserControl
    {
        private Window? _parentWindow;
        private WindowState _winState;

        public WindowState WinState
        {
            get 
            { 
                return _winState; 
            }
            set 
            { 
                SetProperty(ref _winState, value); 
            }
        }



        public Window ParentWindow
        {
            get
            {
                if(_parentWindow == null)
                {
                    _parentWindow = this.Findparent<Window>()!;
                }

                return _parentWindow;
            }
            set
            {
                _parentWindow = value; 
            }
        }

        public TitleBar()
        {
            InitializeComponent();
            btn_Exit.Click += Btn_Exit_Click;
            btn_Maximize.Click += Btn_Maximize_Click;
            btn_Minimize.Click += Btn_Minimize_Click;
        }

        private void Btn_Minimize_Click(object sender, RoutedEventArgs e)
        {

            WinState = WindowState.Minimized;
            ParentWindow.WindowState = WinState;
        }

        private void Btn_Maximize_Click(object sender, RoutedEventArgs e)
        {
            
            WinState = ParentWindow.WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;
            ParentWindow.WindowState = WinState;    
        }

        private async void Btn_Exit_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.Close();


        }
    }
}
