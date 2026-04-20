using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using WPFLib.Controls;

namespace HAHATalk.Stores
{
    // 
    public class MainNavigationStore
    {
        // 현재 화면을 나타내는 화면 데이터 
        // 필드 추가 
        private INotifyPropertyChanged _currentViewModel;   // 2026.03.17 Add Field (_currentViewModel) 
        public INotifyPropertyChanged CurrentViewModel
        {
            get => _currentViewModel;   // 2026.03.17 Add (Get) 
            set
            {
                // 화면이 바뀌면 변수에 저장하고 
                _currentViewModel = value;
                CurrentViewModelChanged?.Invoke(value);  // 이벤트 발생 // 메인화면의 UI가 변경된다 
            }
        }

        // 애니메이션 타입 변수 
        private SlideType _slideType;  // 2026.03.17 Add Field (_slideType)
        public SlideType SlideType
        {
            get => _slideType;  // 2026.03.17 Add (get)
            set
            {
                // 애니메이션 타입을 저장 
                _slideType = value;
                SlideTypeChanged?.Invoke(value); // 값만 전달하고 저장(Backing Field)는 안한다??

            }
        }

        public event Action<INotifyPropertyChanged>? CurrentViewModelChanged;
        public event Action<SlideType>? SlideTypeChanged;
    }
}
