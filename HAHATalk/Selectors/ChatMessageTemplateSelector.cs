using System.Windows;
using System.Windows.Controls;
using CommonLib.Models;

namespace HAHATalk.Selectors
{
    // 메시지 타입(텍스트, 이미지 등)에 따라 적절한 UI 템플릿을 선택하는 클래스 

    public class ChatMessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MyTextTemplate { get; set; }
        public DataTemplate OtherTextTemplate { get; set; }
        public DataTemplate MyImageTemplate { get; set; }
        public DataTemplate OtherImageTemplate { get; set; }
        public DataTemplate MyFileTemplate { get; set; }
        public DataTemplate OtherFileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var message = item as ChatMessage;
            if (message == null) return null;

            if (message.IsMine)
            {
                if (message.MessageType == 1) return MyImageTemplate;
                if (message.MessageType == 2) return MyFileTemplate;
                return MyTextTemplate;
            }
            else
            {
                if (message.MessageType == 1) return OtherImageTemplate;
                if (message.MessageType == 2) return OtherFileTemplate;
                return OtherTextTemplate;
            }
        }
    }

}
