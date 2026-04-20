using CommonLib.Validations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommonLib.Models;

using HAHATalk.Services;

using System.Windows.Media;
using CommonLib.Dtos;


namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class ChangePwdControlViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IAccountService _accountService;
        
        // Property 
        [ObservableProperty]
        private string _email = default!;  // 이메일 

        [ObservableProperty]
        private string _oldPassword = default!; // 이전 비밀번호

        [ObservableProperty]
        private string _newPassword = default!; // 변경 비밀번호

        [ObservableProperty]
        private string _emailValidationText = default!; // 이메일 유효성 체크 결과 표시 텍스트 

        [ObservableProperty]
        public Brush _emailValidationTextColor = default!; // 이메일 유효성 체크 결과 텍스트 표시 컬러 (상태에 따라 변경됨) 

        [ObservableProperty]
        private string _validationText = ""; // 유효성 체크 결과를 나타내는 텍스트 

        private Dictionary<string, bool> _validatindDict;   // 유효성 체크 결과 사전 
        
        private Dictionary<string, bool> ValidatingDict
        {
            // 유효성 체크 결과만 가져오면 되기 때문에 
            // private 및 get 만 설정 
            get
            {
                if(_validatindDict == null)
                {
                    _validatindDict = new Dictionary<string, bool>();
                }

                return _validatindDict;
            }
        }


        public ChangePwdControlViewModel(INavigationService navigationService, IAccountService accountService)
        {
            this._navigationService = navigationService;
            this._accountService = accountService;
        }

        // 2026.03.13 CSJ 비밀번호 변경
        // 2026.04.10 비동기로 변경
        [RelayCommand]
        private async Task ChangePwd()
        {       
            //1. 이메일이 안쓰인 경우 
            //2. 이메일이 유효하지 않은 경우 (계정) 
            //3. 패스워드 체크 

            // 유효성 체크
            if(!await IsValidUpdatePwd())
            {
                return;
            }

            // 저장
            bool isSuccess = await Update_Password();

            if (isSuccess)
            {
                // 변경이 완료 되면 로그인 화면으로 전환 
                _navigationService.Navigate(NaviType.Login);
            }
            else
            {
                ValidationText = "비밀번호 변경 실패. 현재 비밀 번호를 확인하세요.";
            }           
        }

        // 비밀번호를 업데이트 하기 전에 유효성 체크 
        private async Task<bool> IsValidUpdatePwd()
        {
            ClearValidating();

            // 이메일 형식 / 존재여부 체크 
            var emailStatus = await GetEmailValidation();
            
            if(emailStatus != EmailValidationType.AlreadyExists)
            {
                SetValidating(emailStatus == EmailValidationType.FormatError ? "Email" : "NotExistEmail");
                return false;
            }

            // 이전 비밀번호 체크 
            if(string.IsNullOrWhiteSpace(OldPassword))
            {
                SetValidating("OldPassword");
                return false;
            }

            // 변경 비밀번호 체크 
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                SetValidating("NewPassword");
                return false;
            }

            // 이전 비밀번호 = 변경후 비밀번호가 동일하면 
            if (OldPassword == NewPassword)
            {
                SetValidating("SamePassword");
                return false;
            }

            return true;
        }

        // key에 따른 유효성 체크 결과 텍스트 설정 함수 
        private void SetValidating(string key)
        {
            ValidatingDict[key] = true;

            switch (key)
            {
                case "Email":
                    ValidationText = "Email을 입력하세요.";
                    break;
                case "NotExistEmail":
                    ValidationText = "이미 존재하는 Email입니다.";
                    break;

                case "OldPassword":
                    ValidationText = "현재 비밀번호를 입력하세요.";
                    break;
                case "NewPassword":
                    ValidationText = "신규 비밀번호를 입력하세요.";
                    break;
                case "SamePassword":
                    ValidatingDict["OldPassword"] = true;
                    ValidatingDict["NewPassword"] = true;

                    ValidationText = "동일한 비밀번호를 입력하였습니다.";
                    break;
            }

            OnPropertyChanged(nameof(ValidatingDict));

        }

        // 유효성 체크 Text -> Clear 
        private void ClearValidating()
        {
            ValidatingDict.Clear();
            ValidationText = "";
        }


        private async Task<bool> Update_Password()
        {
            // 서버 API -> 변경요청
            // loginInfo에 기존 비번을 담고, parameter 추가 (신규비번)

            var updateDto = new PasswordUpdateDto
            {
                Email = this.Email,
                OldPassword = this.OldPassword,
                NewPassword = this.NewPassword,
            };

            return await _accountService.UpdatePasswordAsync(updateDto);
        }

        private Account GetAccount() => new()
        {
            Email = Email, 
            Pwd = OldPassword,
        };

        // Community Toolkit에 따라 결과가 달라진다 
        // 2026.04.10 이메일 입력 할때 실시간 체크 (서버 통신 발생)
        partial void OnEmailChanged(string value)
        {
            _ = SetEmailValidation(value);
        }


        // 이메일 유효성 체크 
        private async Task SetEmailValidation(string email)
        {
            // 1. "" 여부 체크 
            if(string.IsNullOrWhiteSpace(email))
            {
                EmailValidationText = "";
                return;
            }

            var result = await GetEmailValidation();

            // 2. 입력한 이메일이 이메일 형식에 맞는지 체크 
            switch(result)
            {
                case EmailValidationType.FormatError:
                    EmailValidationText = "이메일 형식에 맞지 않습니다.";
                    EmailValidationTextColor = Brushes.Red;
                    break;
                case EmailValidationType.AlreadyExists:
                    EmailValidationText = "등록된 이메일입니다.";
                    EmailValidationTextColor = Brushes.Blue;
                    break;
                default:
                    EmailValidationText = "존재하지 않은 이메일입니다.";
                    EmailValidationTextColor = Brushes.Red;
                    break;
            }
        }

        private async Task<EmailValidationType> GetEmailValidation()
        {
            if (!DataValidation.IsValidEmail(Email))
            {
                return EmailValidationType.FormatError;
            }

            // 서버 API를 통해 이메일 존재 확인 
            // AccountService->GetAccountAsync -> null이 아니면 이메일 존재

            var account = await _accountService.GetAccountAsnyc(Email);

            // 객체의 상태에 따라 결과 매칭 
            return account switch
            {
                not null    => EmailValidationType.AlreadyExists,
                _           => EmailValidationType.None
            };
        }


        enum EmailValidationType
        {
            None,
            AlreadyExists,
            FormatError
        }

      
    }
}
