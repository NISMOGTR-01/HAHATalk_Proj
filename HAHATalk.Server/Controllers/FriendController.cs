using Microsoft.AspNetCore.Mvc;
using HAHATalk.Server.Repository;
using CommonLib.Models;
using CommonLib.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.SignalR;
using HAHATalk.Server.Hubs;
using Serilog;

namespace HAHATalk.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FriendController : ControllerBase
    {
        private readonly IFriendRepository _friendRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IHubContext<ChatHub> _hubContext; 

        public FriendController(IFriendRepository friendRepository,
            IAccountRepository accountRepository,
            IChatRepository chatRepository,
            IHubContext<ChatHub> hubContext)
        {
            _friendRepository = friendRepository;
            _accountRepository = accountRepository;
            _chatRepository = chatRepository;
            _hubContext = hubContext;
        }

        // 친구 목록 가져오기 (GET api/Friend/{myId})
        [HttpGet("list/{myId}")]
        public async Task<IActionResult> GetFriends(string myId)
        {
            try
            {
                var friends = await _friendRepository.MSSQL_GetFriendsAsync(myId);
                return Ok(friends);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Friend] 목록 조회 중 오류 (MyId: {MyId})", myId);
                return StatusCode(500, "서버 내부 오류 발생");
            }
        }

        // 친구 추가 등록 (POST api/Friend/add)
        [HttpPost("add")]
        public async Task<IActionResult> AddFriend([FromBody] AddFriendRequestDto dto)
        {
            try
            {
                // [검증 1] 상대방 계정이 실제로 존재하는지 확인 (Dapper)
                bool isExist = await _accountRepository.MSSQL_ExistEmailAsync(dto.TargetEmail);
                if (!isExist)
                {
                    return BadRequest("해당 이메일을 사용하는 사용자가 존재하지 않습니다.");
                }

                // [검증 2] 이미 친구인지 확인 (Dapper)
                bool isAlreadyFriend = await _friendRepository.IsFriendAlreadyExistsAsync(dto.MyEmail, dto.TargetEmail);
                if (isAlreadyFriend)
                {
                    return BadRequest("이미 등록된 친구입니다.");
                }

                // [실행 1] 친구 테이블에 추가
                bool addSuccess = await _friendRepository.AddFriendAsync(
                    dto.MyEmail,
                    dto.TargetEmail,
                    dto.FriendName,
                    dto.StatusMessage ?? ""
                );

                if (addSuccess)
                {
                    // [실행 2] 채팅방 ID 생성 (알파벳 정렬 순)
                    var emailList = new List<string> { dto.MyEmail, dto.TargetEmail };
                    emailList.Sort();
                    string roomId = string.Join("_", emailList);

                    // [실행 3] 양방향 채팅 목록 생성/업데이트
                    // 내 닉네임을 가져와서 상대방 방 목록에 표시될 이름을 준비합니다.
                    var myAccount = await _accountRepository.MSSQL_GetAccountByEmailAsync(dto.MyEmail);

                    var initialMsg = new ChatMessageDto
                    {
                        RoomId = roomId,
                        Message = "새로운 친구와 대화를 시작해보세요!",
                        SendTime = DateTime.Now
                    };

                    // ChatRepository의 Dapper 로직을 사용하여 양방향 데이터를 Insert/Update 합니다.
                    await _chatRepository.MSSQL_UpdateChatListAsync(
                        initialMsg,
                        dto.TargetEmail, dto.FriendName, // 내가 보는 상대방 이름
                        dto.MyEmail, myAccount?.Nickname ?? dto.MyEmail // 상대방이 보는 내 이름
                    );

                    // [실행 4] 실시간 알림 (SignalR)
                    await _hubContext.Clients.User(dto.TargetEmail).SendAsync("UpdateChatList");

                    Log.Information("[Friend] 친구 추가 완료: {MyId} -> {Target}", dto.MyEmail, dto.TargetEmail);
                    return Ok(true);
                }

                return BadRequest("친구 등록 처리에 실패했습니다.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Friend] 추가 중 예외 발생: {MyId} -> {Target}", dto.MyEmail, dto.TargetEmail);
                return StatusCode(500, "서버 오류가 발생했습니다.");
            }
        }


        // 친구 중복 확인 (GET api/Friend/exists)
        [HttpGet("exists/{myId}/{friendEmail}")]
        public async Task<IActionResult> IsFriendExists(string myId, string friendEmail)
        {
            try
            {
                bool exists = await _friendRepository.IsFriendAlreadyExistsAsync(myId, friendEmail);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Friend] 중복 체크 오류: {MyId}, {Friend}", myId, friendEmail);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
