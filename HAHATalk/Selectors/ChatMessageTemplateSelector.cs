using System.Windows;
using System.Windows.Controls;
using CommonLib.Models;
using CommonLib.Enums;

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

            // 디버깅용: 출력창에서 특정 메시지의 타입을 확인해보세요.
            // System.Diagnostics.Debug.WriteLine($"Msg: {message.Message}, Type: {message.MessageType}");

            // MessageType이 Image(1)이거나, 타입이 File(3)이어도 확장자가 이미지면 이미지 템플릿 사용
            bool isActuallyImage = (message.MessageType == (int)ChatMessageTypes.Image) ||
                                  (message.MessageType == (int)ChatMessageTypes.File && message.IsImage);

            if (message.IsMine)
            {
                if (isActuallyImage) 
                    return MyImageTemplate;
                if (message.MessageType == (int)ChatMessageTypes.File) 
                    return MyFileTemplate;

                return MyTextTemplate;
            }
            else
            {
                if (isActuallyImage) 
                    return OtherImageTemplate;
                if (message.MessageType == (int)ChatMessageTypes.File) 
                    return OtherFileTemplate;

                return OtherTextTemplate;
            }
        }
    }

}
