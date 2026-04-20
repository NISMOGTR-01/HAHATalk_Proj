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

namespace WPFLib.Controls
{
    /// <summary>
    /// TextBoxControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TextBoxControl : UserControl
    {
        public TextBoxControl()
        {
            InitializeComponent();
        }

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
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        #endregion

        #region Public Statics 
        // Using a DependencyProperty as the backing store for BorderBrush.  This enables animation, styling, binding, etc...
        public static new readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(TextBoxControl), new UIPropertyMetadata(Brushes.SkyBlue));


      

        // Using a DependencyProperty as the backing store for BorderThickness.  This enables animation, styling, binding, etc...
        public static new readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(TextBoxControl), new UIPropertyMetadata(new Thickness(1)));


      

        public static readonly DependencyProperty WaterMarkTextProperty =
            DependencyProperty.Register(nameof(WaterMarkText), typeof(string), typeof(TextBoxControl), new PropertyMetadata(null));



        public static readonly DependencyProperty WaterMarkTextColorProperty =
            DependencyProperty.Register(nameof(WaterMarkTextColor), typeof(Brush), typeof(TextBoxControl), new UIPropertyMetadata(Brushes.Gray));




   
        // FrameworkPropertyMedata 사용 : two way 방식 사용 
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBoxControl), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

 
        public static readonly DependencyProperty ValidatingProperty =
            DependencyProperty.Register(nameof(Validating), typeof(bool), typeof(TextBoxControl), new UIPropertyMetadata(false));

        #endregion
    }
}
