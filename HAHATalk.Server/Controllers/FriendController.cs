using Microsoft.AspNetCore.Mvc;
using HAHATalk.Server.Repositories;
using CommonLib.Models;
using CommonLib.Dtos;
using HAHATalk.Server.Data; // AppDbContext 
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using HAHATalk.Server.Hubs;

namespace HAHATalk.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FriendController : ControllerBase
    {
        private readonly IFriendRepository _friendRepository;
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext; 

        public FriendController(IFriendRepository friendRepository, AppDbContext context, IHubContext<ChatHub> hubContext)
        {
            this._friendRepository = friendRepository;
            this._context = context;
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
                return StatusCode(500, $"Internal server error : {ex.Message}");
            }
        }

        // 친구 추가 등록 (POST api/Friend/add)
        [HttpPost("add")]
        public async Task<IActionResult> AddFriend([FromBody] AddFriendRequestDto dto)
        {
            try
            {
                // 실존 계정 체크 
                var targetAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == dto.TargetEmail);

                if(targetAccount == null)
                {
                    return BadRequest("해당 이메일을 사용하는 사용자가 존재하지 않습니다.");
                }

                // 이미 친구인지 확인
                bool exists = await _friendRepository.IsFriendAlreadyExistsAsync(dto.MyEmail, dto.TargetEmail);

                if(exists)
                {
                    return BadRequest("이미 등록된 친구입니다.");
                }

                // 기존 Repository를 통한 친구 추가 
                bool isResult = await _friendRepository.AddFriendAsync(
                    dto.MyEmail, 
                    dto.TargetEmail,
                    dto.FriendName, 
                    dto.StatusMessage ?? ""
                    );

                if(isResult)
                {
                    // 실시간 채팅을 위한 ChatList(방 목록) 생성 
                    var emailList = new List<string>
                    {
                        dto.MyEmail, dto.TargetEmail
                    };
                    emailList.Sort();
                    string roomId = string.Join("_", emailList);

                    // 내 시점의 채팅 목록 생성 
                    var myChat = await _context.ChatLists.FirstOrDefaultAsync(c => c.RoomId == roomId && c.OwnerId == dto.MyEmail);

                    if (myChat == null)
                    {
                        _context.ChatLists.Add(new Models.ChatList
                        {
                            RoomId = roomId, 
                            OwnerId = dto.MyEmail, 
                            TargetId = dto.TargetEmail, 
                            TargetName = dto.FriendName, 
                            LastMessage = "새로운 친구와 대화를 시작해보세요!", 
                            LastTime = DateTime.Now,
                            UnreadCount = 0,
                        });
                    }

                    // 상대방 시점의 채팅 목록 생성 
                    var targetChat = await _context.ChatLists.FirstOrDefaultAsync(c => c.RoomId == roomId && c.OwnerId == dto.TargetEmail);

                    if(targetChat == null)
                    {
                        var myAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == dto.MyEmail);
                        _context.ChatLists.Add(new Models.ChatList
                        {
                            RoomId = roomId,
                            OwnerId = dto.TargetEmail,
                            TargetId = dto.MyEmail,
                            TargetName = myAccount?.Nickname ?? dto.MyEmail,
                            LastMessage = "새로운 친구와 대화를 시작해보세요!",
                            LastTime = DateTime.Now,
                            UnreadCount = 0
                        });
                    }

                    // ChatLIst 변경 사항 최종 저장 
                    await _context.SaveChangesAsync();

                    // 상대방에게 실시간 알림 전송 
                    // 상대방이 로그인 중인경우 즉시 채팅 목록을 갱신하라는 신호 
                    await _hubContext.Clients.User(dto.TargetEmail).SendAsync("UpdateChatList");

                    return Ok(true);
                }

                return BadRequest("친구 등록에 실패했습니다.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"서버 오류: {ex.Message}");
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
                return StatusCode(500, $"Internal server error : {ex.Message}");
            }
        }
    }
}
