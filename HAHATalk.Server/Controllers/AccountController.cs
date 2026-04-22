using CommonLib.Dtos;
using CommonLib.Models;
using HAHATalk.Server.Repository;
using HAHATalk.Server.Security;
using Microsoft.AspNetCore.Mvc;
using Serilog;


namespace HAHATalk.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        // IAccountRepository 사용 
        private readonly IAccountRepository _accountRepository;

        public AccountController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        // 로그인체크 (POST 방식 활용) 
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginInfo)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(new LoginResponseDto
                {
                    IsSuccess = false,
                    Message = "입력 형식이 잘못되었습니다."
                });
            }

            try
            {
                bool isSuccess = await _accountRepository.MSSQL_Login_CheckAsync(loginInfo.Email, loginInfo.Pwd);

                if (isSuccess)
                {
                    var user = await _accountRepository.MSSQL_GetAccountByEmailAsync(loginInfo.Email);

                    if (user != null)
                    {
                        user.Pwd = string.Empty; // 보안상 비밀번호 비움

                        Log.Information("[Login Success] Email: {Email}", loginInfo.Email);

                        return Ok(new LoginResponseDto
                        {
                            IsSuccess = true,
                            Message = "반갑습니다!",
                            UserAccount = user
                        });
                    }
                }

                Log.Warning("[Login Failed] Email: {Email}", loginInfo.Email);
                return Ok(new LoginResponseDto { IsSuccess = false, Message = "이메일 또는 비밀번호가 틀렸습니다." });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Login] 로그인 중 예외 발생 (Email: {Email})", loginInfo.Email);
                return StatusCode(500, "서버 내부 오류");
            }
        }

        // 회원가입 
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Account newAccount)
        {
            try
            {
                // 이메일 중복 체크
                if (await _accountRepository.MSSQL_ExistEmailAsync(newAccount.Email))
                {
                    return BadRequest("이미 존재하는 이메일입니다.");
                }

                long result = await _accountRepository.MSSQL_SaveAsync(newAccount, newAccount.Pwd);

                if (result > 0)
                {
                    Log.Information("[Register] 회원가입 성공: {Email}", newAccount.Email);
                    return Ok(true);
                }

                return BadRequest("회원가입에 실패했습니다.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Register] 회원가입 중 예외 발생: {Email}", newAccount.Email);
                return StatusCode(500, "서버 오류");
            }
        }

        // 이메일 정보 가져오기 
        [HttpGet("{email}")]
        public async Task<IActionResult> GetAccount(string email)
        {
            var account = await _accountRepository.MSSQL_GetAccountByEmailAsync(email);
            if (account == null)
            {
                return NotFound();
            }

            account.Pwd = string.Empty; // 보안
            return Ok(account);
        }

        // 비밀번호 변경 
        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] PasswordUpdateDto data )
        {
            if (!ModelState.IsValid)
                return BadRequest("데이터 형식이 맞지 않습니다.");

            try
            {
                var account = await _accountRepository.MSSQL_GetAccountByEmailAsync(data.Email);

                if (account == null)
                    return Ok(false);

                // 현재 비밀번호 검증 (SecurityHelper 활용)
                if (!SecurityHelper.VerifyPassword(data.OldPassword, account.Pwd))
                {
                    Log.Warning("[UpdatePassword] 비밀번호 불일치: {Email}", data.Email);
                    return Ok(false);
                }

                var result = await _accountRepository.MSSQL_Pass_UpdateAsync(account, data.NewPassword);

                if (result > 0)
                    Log.Information("[UpdatePassword] 비밀번호 변경 완료: {Email}", data.Email);

                return Ok(result > 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UpdatePassword] 변경 중 예외 발생: {Email}", data.Email);
                return StatusCode(500, "서버 오류");
            }
        }
        
        // 메일 체크 
        [HttpGet("exist/{email}")]
        public async Task<IActionResult> ExistEmail(string email)
        {
            // Repository에 MSSQL_ExistEmail(email) 호출 
            bool exists = await _accountRepository.MSSQL_ExistEmailAsync(email);

            // true or false 를 JSON 형태로 반환 
            return Ok(exists);
        }

        // 계정찾기 
        [HttpPost("find-account")]
        public async Task<IActionResult> FindAccount([FromBody] Account account)
        {
            // Repository 비동기 메소드 호출 
            string? email = await _accountRepository.MSSQL_Find_AccountAsync(account);

            if(string.IsNullOrEmpty(email) || email == "0")
            {
                return Ok("0");
            }

            // 클라이언트에서 ReadAsStringAsync로 읽어들이므로 이메일 문자열 그대로 반환 
            return Ok(email);
        }

    }
}
