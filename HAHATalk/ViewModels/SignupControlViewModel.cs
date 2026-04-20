using CommonLib.Validations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommonLib.Models;
using HAHATalk.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HAHATalk.ViewModels
{
    [ObservableObject]
    public partial class SignupControlViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IAccountService _accountService;

        [ObservableProperty]
        private string _email = default!;

        [ObservableProperty]
        private string _nickname = default!;

        [ObservableProperty]
        private string _cellPhone = default!;

        [ObservableProperty]
        private string _password = default!;

        [ObservableProperty]
        private string _passwordConfirm = default!;

        [ObservableProperty]
        private string _emailValidationText = default!;

        [ObservableProperty]
        private string _validationText = "";

        [ObservableProperty]
        public Brush _emailValidationTextColor = default!;

        private Dictionary<string, bool> _validatingDict;
        private Dictionary<string, bool> ValidatingDict
        {
            get
            {
                if(_validatingDict == null)
                {
                    _validatingDict = new Dictionary<string, bool>();
                }

                return _validatingDict;
            }
        }

        public SignupControlViewModel(INavigationService navigationService, 
            IAccountService accountService)
        {
            this._navigationService = navigationService;
            this._accountService = accountService;
        }


        [RelayCommand]
        private async Task Signup() // 2026.04.10 비동기로 변경 
        {
            // 유효성 체크 
            if(!await IsValidSignup())
            {
                return;
            }

            // 저장 (서버 API 호출) 
            bool isSaved = await Save();

            if(isSaved)
            {
                // 로그인 타입 
                _navigationService.Navigate(NaviType.Login);

            }
            else 
            {
                ValidationText = "서버 연결 오류 또는 가입 정보 중복으로 실패했습니다.";
            } 
                

        }

        private async Task<bool> Save()
        {
            Account account = GetAccount();

            // 서버의 AccountControll
            return await _accountService.RegisterAsync(account);

        }

        private Account GetAccount() => new()
        {
            Email = Email,
            Nickname = Nickname,
            CellPhone = CellPhone, 
            Pwd = Password, // 서버에서 SecurityHelper.HashPassword를 거칠 예정 
        };

        private void ClearValidating()
        {
            ValidatingDict.Clear();
            ValidationText = "";
        }

        private void SetValidating(string key)
        {
            ValidatingDict[key] = true;

            switch (key)
            {
                case "Email":                    
                    ValidationText = "Email을 입력하세요.";
                    break;
                case "ExistEmail":
                    ValidationText = "이미 존재하는 Email입니다.";
                    break;
                case "Nickname":
                    ValidationText = "닉네임을 입력하세요.";
                    break;
                case "CellPhone":
                    ValidationText = "휴대전화번호를 입력하세요.";
                    break;
                case "Password":
                    ValidationText = "비밀번호를 입력하세요.";
                    break;
                case "PasswordConfirm":
                    ValidationText = "비밀번호 확인을 입력하세요.";
                    break;
                case "DifferentPassword":
                    ValidatingDict["Password"] = true;
                    ValidatingDict["PasswordConfirm"] = true;
                    
                    ValidationText = "비밀번호가 일치하지 않습니다. 다시 확인해주세요.";
                    break;
            }

            OnPropertyChanged(nameof(ValidatingDict));

        }


        // 유효성 체크
        // 비동기로 변경 
        private async Task<bool> IsValidSignup()
        {
            ClearValidating();

            if (string.IsNullOrWhiteSpace(Email)) 
            {
                SetValidating("Email"); 
                return false; 
            }

            // 실제 데이터에 이메일이 존재하는지 
            // 서버API를 통한 이메일 중복 체크 
            if (await _accountService.ExistEmailAsync(Email))
            {
                SetValidating("ExistEmail");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Nickname)) 
            {
                SetValidating("Nickname"); 
                return false; 
            }
            if (string.IsNullOrWhiteSpace(CellPhone)) 
            {
                SetValidating("CellPhone"); 
                return false; 
            }
            if (string.IsNullOrWhiteSpace(Password)) 
            { 
                SetValidating("Password"); 
                return false; 
            }
            if (string.IsNullOrWhiteSpace(PasswordConfirm)) 
            { 
                SetValidating("PasswordConfirm"); 
                return false; 
            }

            if (Password != PasswordConfirm)
            {
                SetValidating("DifferentPassword");
                return false;
            }

            return true;
        }

        // Xaml Interaction > TextChanged > Command > CommandProperty > RelayCommand > func
        // 이메일 입력 시 실시간 체크 부분 
        partial void OnEmailChanged(string value)
        {
            // 실시간 체크는 UI 반응성을 위해 별도 Task로 관리하거나, 
            // 간단하게 Fire and Forget으로 처리 (실무에선 Debounce 권장)
            _ = SetEmailValidation(value);
        }

        // 2026.04.10 비동기로 변환
        private async Task<EmailValidationType> GetEmailValidation()
        {
           if(!DataValidation.IsValidEmail(Email))
            {
                return EmailValidationType.FormatError;
            }

            // MYSQL
            // if(_accountRepository.ExistEmail(Email))
            // MSSQL 
            if(await _accountService.ExistEmailAsync(Email))            
            {
                return EmailValidationType.AlreadyExists;
            }

            return EmailValidationType.None;
        }

        // 이메일 유효성 체크 
        private async Task SetEmailValidation(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                EmailValidationText = "";
                return;
            }

            // 입력한 이메일이 형식에 맞는지 체크 
            var validationType = await GetEmailValidation();

            switch (validationType)
            {
                case EmailValidationType.FormatError:
                    EmailValidationText = "이메일 형식에 맞지 않습니다.";
                    EmailValidationTextColor = Brushes.Red;
                    break;
                case EmailValidationType.AlreadyExists:
                    EmailValidationText = "이미 존재하는 이메일입니다.";
                    EmailValidationTextColor = Brushes.Red;
                    break;
                default:
                    EmailValidationText = "사용할 수 있는 이메일입니다.";
                    EmailValidationTextColor = Brushes.Blue;
                    break;
            }

        }



        
        enum EmailValidationType
        {
            None, 
            AlreadyExists, 
            FormatError
        }

     
    }
}
