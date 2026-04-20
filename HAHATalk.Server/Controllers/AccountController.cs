using Microsoft.AspNetCore.Mvc;
using HAHATalk.Server.Repositories;
using CommonLib.Models;
using HAHATalk.Server.Repository;
using CommonLib.Dtos;
using HAHATalk.Server.Security;


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

            bool isSuccess = await _accountRepository.MSSQL_Login_CheckAsync(loginInfo.Email, loginInfo.Pwd);

            if (isSuccess)
            {
                // 2. 로그인 성공 시 사용자 정보 가져오기
                var user = await _accountRepository.MSSQL_GetAccountByEmailAsync(loginInfo.Email);

                if (user != null)
                {
                    // [보안] 클라이언트에 비번을 보내면 안 되니 비웁니다.
                    user.Pwd = string.Empty;

                    Console.WriteLine($"[Login Success] Email: {loginInfo.Email}");

                    return Ok(new LoginResponseDto
                    {
                        IsSuccess = true,
                        Message = "반갑습니다!",
                        UserAccount = user
                    });
                }
            }

            Console.WriteLine($"[Login Failed] Email: {loginInfo.Email}");
            return Ok(new LoginResponseDto { IsSuccess = false, Message = "이메일 또는 비밀번호가 틀렸습니다." });
        }

        // 회원가입 
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Account newAccount)
        {
            // 이메일 중복 체크 
            if(await _accountRepository.MSSQL_ExistEmailAsync(newAccount.Email))
            {
                return BadRequest("이미 존재하는 이메일입니다.");
            }

            long result = await _accountRepository.MSSQL_SaveAsync(newAccount, newAccount.Pwd);

            if(result > 0)
            {
                return Ok(true);
            }

            return BadRequest("회원가입에 실패했습니다.");
        }

        // 이메일 정보 가져오기 
        [HttpGet("{email}")]
        public async Task<IActionResult> GetAccount(string email)
        {
            var account = await _accountRepository.MSSQL_GetAccountByEmailAsync(email);
            if(account == null)
            {
                return NotFound();
            }

            return Ok(account);
        }

        // 비밀번호 변경 
        [HttpPost("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] PasswordUpdateDto data )
        {
            if (!ModelState.IsValid) 
                return BadRequest("데이터 형식이 맞지 않습니다.");

            var account = await _accountRepository.MSSQL_GetAccountByEmailAsync(data.Email);
            
            if (account == null) 
                return Ok(false);

            if (!SecurityHelper.VerifyPassword(data.OldPassword, account.Pwd))
            {
                return Ok(false); // 현재 비번 틀림
            }

            var result = await _accountRepository.MSSQL_Pass_UpdateAsync(account, data.NewPassword);
            return Ok(result > 0);
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
