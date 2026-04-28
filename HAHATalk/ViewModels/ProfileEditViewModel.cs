using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAHATalk.Services;
using HAHATalk.Stores;
using Microsoft.Win32;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks;
using System.Windows;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class ProfileEditViewModel
    {
        private readonly IAccountService _accountService;
        private readonly UserStore _userStore;

        // 편집용 데이터 (이름, 상태메세지) 
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NameLengthText))]
        private string _editName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusLengthText))]
        private string _editStatus = string.Empty;

        // 이미지 관련 
        [ObservableProperty]
        private string _displayProfileImg = string.Empty; // 화면 표시용 (URL or 로컬경로) 

        private string? _localFilePath; // 실제 업로드할 로컬 파일 경로 

        // 글자 수 표시 텍스트 
        public string NameLengthText => $"{EditName?.Length ?? 0}/20";
        public string StatusLengthText => $"{EditStatus?.Length ?? 0}/60";

        // 생성자 
        public ProfileEditViewModel(IAccountService accountService, UserStore userStore, 
            string currentName, string currentStatus, string currentImg)
        {
            _accountService = accountService;
            _userStore = userStore;

            EditName = currentName;
            EditStatus = currentStatus;
            DisplayProfileImg = currentImg;
        }

        //카메라 아이콘 클릭 - 이미지 선택 
        [RelayCommand]
        private void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp", 
                Title = "프로필 이미지 선택"
            };

            if(dialog.ShowDialog() == true)
            {
                _localFilePath = dialog.FileName;
                DisplayProfileImg = _localFilePath; // 미리 보기 반영 
            }
        }

        // 확인버튼 - 서버 전송 
        [RelayCommand]
        private async Task Confirm(object obj)
        {
            if(string.IsNullOrWhiteSpace(EditName))
            {
                MessageBox.Show("이름은 비워둘 수 없습니다.");
                return;
            }

            try
            {
                string myEmail = _userStore.CurrentUserEmail ?? "";

                // 서비스 호출 (Multipart 전송) 
                // 이름 변경 기능은 추후 API확장시 추가 , 현재는 이미지 / 상태메세지 우선 업데이트 
                bool isSuccess = await _accountService.UpdateProfileAsync(myEmail, _localFilePath, EditStatus);

                if(isSuccess)
                {
                    if(obj is Window window)
                    {
                        window.DialogResult = true; // 성공 알림용 
                        window.Close();
                    }
                }
                else
                {
                    MessageBox.Show("프로필 수정에 실패했습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}");
            }
        }
       

        // 취소/ 닫기 버튼 
        [RelayCommand]
        private void Close(object obj)
        {
            if(obj is Window window)
            {
                window.Close();
            }
        }
    }
}
