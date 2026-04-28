using CommonLib.Dtos;
using CommonLib.Models;
using HAHATalk.Server.Repository;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Formats.Asn1;


namespace HAHATalk.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatRepository _chatRepository;

        public ChatController(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        // 채팅 목록 가져오기
        [HttpGet("list/{email}")]
        public async Task<IActionResult> GetChatList(string email)
        {
            try
            {
                var list = await _chatRepository.MSSQL_GetChatListAsync(email);
                return Ok(list);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Chat] 목록 조회 실패 (Email: {Email})", email);
                return StatusCode(500, "목록을 가져오는 중 오류가 발생했습니다.");
            }

        }

        // 특정 방의 메세지 내역 가져오기(채팅방 입장)
        [HttpGet("history/{roomId}")]
        public async Task<IActionResult> GetChatHistory(string roomId)
        {
            try
            {
                var history = await _chatRepository.MSSQL_GetMessageByRoomIdAsync(roomId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Chat] 대화 내역 조회 실패 (RoomId: {RoomId})", roomId);
                return StatusCode(500, "대화 내역을 가져오는 중 오류가 발생했습니다.");
            }

        }

        // 특정 방의 메세지 저장하기 
        [HttpPost("save")]
        public async Task<ActionResult<bool>> SaveMessage([FromBody] ChatMessageDto data)
        {
            if (data == null)
                return BadRequest("데이터가 비어있습니다.");

            try
            {
                // DTO를실제 DB 모델(ChatMessage)로 변환 (Mapping) 
                var messageEntity = new ChatMessage
                {
                    RoomId = data.RoomId,
                    SenderId = data.SenderId,
                    Message = data.Message,
                    MessageType = data.MessageType,
                    SendTime = data.SendTime,
                    FilePath = data.FilePath,
                    FileName = data.FileName,
                    IsRead = false,
                    MessageGuid = data.MessageGuid,
                };

                // Repository 를 통해서 저장 
                var saveResult = await _chatRepository.MSSQL_SaveMessageAsync(messageEntity);

                // 성공 시 True 반환 (보통 저장된 행의 수가 0보다 크면 성공)
                if (!saveResult)
                {
                    Log.Warning("[Chat] 메시지 저장 실패 (RoomId: {RoomId}, Sender: {Sender})", data.RoomId, data.SenderId);
                    return BadRequest("메세지 저장 실패");
                }

                return Ok(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Chat] 메시지 저장 중 예외 발생");
                return StatusCode(500, ex.Message);
            }
        }

        // 읽지 않은 메세지 총합 가져오기 
        [HttpGet("unread-total/{email}")]
        public async Task<IActionResult> GetTotalUnreadCount(string email)
        {
            try
            {
                var count = await _chatRepository.MSSQL_GetTotalUnreadCountAsync(email);
                return Ok(count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Chat] 안읽은 메시지 카운트 실패 (Email: {Email})", email);
                return Ok(0); // 에러 시에도 앱이 멈추지 않게 0 반환
            }
        }


        // 2026.04.10
        // 채팅 업데이트 
        [HttpPost("update-list")]
        public async Task<IActionResult> UpdateList([FromBody] ChatUpdateDto data)
        {
            if (data == null || data.Message == null)
                return BadRequest("업데이트 데이터가 유효하지 않습니다.");

            try
            {
                var result = await _chatRepository.MSSQL_UpdateChatListAsync(
                    data.Message,
                    data.TargetId,
                    data.TargetName,
                    data.MyId,
                    data.MyNickname
                );

                if (result)
                {
                    Log.Information("[Chat] 채팅 목록 업데이트 완료 (RoomId: {RoomId})", data.Message.RoomId);
                    return Ok(true);
                }

                return BadRequest("목록 업데이트 실패");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Chat] 목록 업데이트 중 예외 발생");
                return StatusCode(500, "서버 업데이트 오류");
            }
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkReadRequestDto request)
        {
            //
            if (request == null || string.IsNullOrEmpty(request.RoomId))
                return BadRequest();

            try
            {
                // Dapper로 리팩토링된 UpdateReadStatusAsync 호출
                var result = await _chatRepository.MSSQL_UpdateReadStatusAsync(request.RoomId, request.UserId);

                if (result)
                    Log.Information("[Chat] 읽음 처리 완료 (Room: {RoomId}, User: {UserId})", request.RoomId, request.UserId);

                return result ? Ok(true) : BadRequest();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Chat] 읽음 처리 중 예외 발생");
                return StatusCode(500, "서버 오류");
            }
        }
    }
}
