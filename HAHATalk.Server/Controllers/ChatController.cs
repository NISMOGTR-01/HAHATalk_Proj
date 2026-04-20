using CommonLib.Models;
using CommonLib.Dtos;
using HAHATalk.Server.Repositories;
using Microsoft.AspNetCore.Mvc;


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
            var list = await _chatRepository.MSSQL_GetChatListAsync(email);
            return Ok(list);
        }

        // 특정 방의 메세지 내역 가져오기(채팅방 입장)
        [HttpGet("history/{roomId}")]
        public async Task<IActionResult> GetChatHistory(string roomId)
        {
            var history = await _chatRepository.MSSQL_GetMessageByRoomIdAsync(roomId);
            return Ok(history);

        }

        // 특정 방의 메세지 저장하기 
        [HttpPost("save")]
        public async Task<ActionResult<bool>> SaveMessage([FromBody] ChatMessageDto data)
        {
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
                };

                // Repository 를 통해서 저장 
                var saveResult = await _chatRepository.MSSQL_SaveMessageAsync(messageEntity);

                // 성공 시 True 반환 (보통 저장된 행의 수가 0보다 크면 성공)
                if(!saveResult)
                {
                    return BadRequest("메세지 저장 실패");
                }

                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // 읽지 않은 메세지 총합 가져오기 
        [HttpGet("unread-total/{email}")]
        public async Task<IActionResult> GetTotalUnreadCount(string email)
        {
            var count = await _chatRepository.MSSQL_GetTotalUnreadCountAsync(email);
            return Ok(count);
        }


        // 2026.04.10
        // 채팅 업데이트 
        [HttpPost("update-list")]
        public async Task<IActionResult> UpdateList([FromBody] ChatUpdateDto data)
        {
            // 이제 데이터가 null 없이 잘 들어올 겁니다.
            if (data == null || data.Message == null) 
                return BadRequest("데이터가 누락되었습니다.");

            try
            {
                // Repository 호출 (이미 규격이 맞으므로 data.Message를 그대로 전달하거나 맵핑)
                var result = await _chatRepository.MSSQL_UpdateChatListAsync(
                    data.Message, // ChatMessageDto 전달
                    data.TargetId,
                    data.TargetName,
                    data.MyId,
                    data.MyNickname
                );

                return result ? Ok(true) : BadRequest("목록 업데이트 실패");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }


            /*
            try
            {
                // ChatMessageDto 형태로 변환하여 Repository에 전달 (규격 맞춤)
                var dto = new ChatMessageDto
                {
                    RoomId = data.Message.RoomId,
                    Message = data.Message.Message,
                    SendTime = data.Message.SendTime,
                    SenderId = data.Message.SenderId
                    // 필요한 다른 필드들도 매핑
                };

                // Repository의 MSSQL_UPdateChatListAsync 호출 
                var result = await _chatRepository.MSSQL_UpdateChatListAsync(
                    dto,
                    data.TargetId,
                    data.TargetName,
                    data.MyId,
                    data.MyNickname
                    );

                if(result)
                {
                    return Ok(data);
                }
                else
                {
                    return BadRequest("채팅 목록 업데이트 실패");
                }               
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
            */
        }      
    }
}
