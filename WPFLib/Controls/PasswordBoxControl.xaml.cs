using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace WPFLib.Controls
{
    /// <summary>
    /// PasswordBoxControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PasswordBoxControl : UserControl
    {
        public PasswordBoxControl()
        {
            InitializeComponent();

            // 이벤트 함수 추가 
            txt.TextChanged += Txt_TextChanged;
            pwd.PasswordChanged += Pwd_PasswordChanged;
        }

        #region Fields 
        private bool _isFirst = true;    // 패스워드를 처음에 입력하였는가 판단 
        #endregion


        #region Methods

        private void Pwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = pwd.Password;
        }

        private void Txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFirst || DesignerProperties.GetIsInDesignMode(this))
            {
                if(pwd.Password != txt.Text)
                {
                    // 패스워드 설정 
                    pwd.Password = txt.Text;
                    // 변수 false 설정 
                    _isFirst = false;
                }
            }
        }

        #endregion

        // 종속성 속성 
        // BorderBrush

        #region Public Properties



        public bool Validating
        {
            get { return (bool)GetValue(ValidatingProperty); }
            set { SetValue(ValidatingProperty, value); }
        }





        public new Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        // WaterMarkText
        public string WaterMarkText
        {
            get { return (string)GetValue(WaterMarkTextProperty); }
            set { SetValue(WaterMarkTextProperty, value); }
        }

        // BorderThickness
        public new Thickness BorderThickness
        {
            get { return (Thickness)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        // WaterMarkTextColor
        public Brush WaterMarkTextColor
        {
            get { return (Brush)GetValue(WaterMarkTextColorProperty); }
            set { SetValue(WaterMarkTextColorProperty, value); }
        }

        // Text
        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        #endregion

        #region Public Statics 
        // Using a DependencyProperty as the backing store for BorderBrush.  This enables animation, styling, binding, etc...
        public static new readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(PasswordBoxControl), new UIPropertyMetadata(Brushes.SkyBlue));




        // Using a DependencyProperty as the backing store for BorderThickness.  This enables animation, styling, binding, etc...
        public static new readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(PasswordBoxControl), new UIPropertyMetadata(new Thickness(1)));




        public static readonly DependencyProperty WaterMarkTextProperty =
            DependencyProperty.Register(nameof(WaterMarkText), typeof(string), typeof(PasswordBoxControl), new PropertyMetadata(null));



        public static readonly DependencyProperty WaterMarkTextColorProperty =
            DependencyProperty.Register(nameof(WaterMarkTextColor), typeof(Brush), typeof(PasswordBoxControl), new UIPropertyMetadata(Brushes.Gray));





        // FrameworkPropertyMedata 사용 : two way 방식 사용 
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(nameof(Password), typeof(string), typeof(PasswordBoxControl), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public static readonly DependencyProperty ValidatingProperty =
            DependencyProperty.Register(nameof(Validating), typeof(bool), typeof(PasswordBoxControl), new UIPropertyMetadata(false));

        #endregion
    }
}
