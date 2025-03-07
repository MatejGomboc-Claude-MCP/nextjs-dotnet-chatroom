using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatRoom.Api.Models.DTOs;
using ChatRoom.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ChatRoom.Api.Hubs;

namespace ChatRoom.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessagesController> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessagesController(
            IMessageService messageService, 
            ILogger<MessagesController> logger,
            IHubContext<ChatHub> hubContext)
        {
            _messageService = messageService;
            _logger = logger;
            _hubContext = hubContext;
        }

        // GET: api/messages
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<MessageDto>>> GetMessages(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (page < 1)
                {
                    page = 1;
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    pageSize = 50;
                }

                var result = await _messageService.GetMessagesPagedAsync(page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages");
                return StatusCode(500, "An error occurred while retrieving messages.");
            }
        }

        // GET: api/messages/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MessageDto>> GetMessage(Guid id)
        {
            try
            {
                var message = await _messageService.GetMessageByIdAsync(id);
                return Ok(message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Message with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving message with ID {id}");
                return StatusCode(500, "An error occurred while retrieving the message.");
            }
        }

        // POST: api/messages
        [HttpPost]
        [EnableRateLimiting("message_creation")]
        public async Task<ActionResult<MessageDto>> CreateMessage([FromBody] CreateMessageDto createMessageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var message = await _messageService.CreateMessageAsync(
                    createMessageDto.Text,
                    createMessageDto.Username
                );

                return CreatedAtAction(
                    nameof(GetMessage),
                    new { id = message.Id },
                    message
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message");
                return StatusCode(500, "An error occurred while creating the message.");
            }
        }
        
        // PUT: api/messages/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<MessageDto>> UpdateMessage(Guid id, [FromBody] UpdateMessageDto updateMessageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var message = await _messageService.UpdateMessageAsync(
                    id,
                    updateMessageDto.Text,
                    updateMessageDto.Username
                );
                
                // Broadcast the message update via SignalR
                await _hubContext.Clients.All.SendAsync(
                    "messageEdited", 
                    new { 
                        messageId = message.Id, 
                        text = message.Text,
                        editedAt = message.EditedAt
                    }
                );

                return Ok(message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Message with ID {id} not found.");
            }
            catch (UnauthorizedAccessException ex)
            {
                // Fix: Forbid() doesn't take a message parameter
                _logger.LogWarning($"Unauthorized access: {ex.Message}");
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating message with ID {id}");
                return StatusCode(500, "An error occurred while updating the message.");
            }
        }
        
        // DELETE: api/messages/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(Guid id, [FromQuery] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required for message deletion.");
            }

            try
            {
                var result = await _messageService.DeleteMessageAsync(id, username);
                
                if (!result)
                {
                    return NotFound($"Message with ID {id} not found.");
                }
                
                // Broadcast the message deletion via SignalR
                await _hubContext.Clients.All.SendAsync("messageDeleted", new { messageId = id.ToString() });

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                // Fix: Forbid() doesn't take a message parameter
                _logger.LogWarning($"Unauthorized access: {ex.Message}");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting message with ID {id}");
                return StatusCode(500, "An error occurred while deleting the message.");
            }
        }
        
        // GET: api/messages/search
        [HttpGet("search")]
        public async Task<ActionResult<PagedResultDto<MessageDto>>> SearchMessages(
            [FromQuery] string q, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrEmpty(q))
            {
                return BadRequest("Search query cannot be empty.");
            }

            try
            {
                if (page < 1)
                {
                    page = 1;
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    pageSize = 20;
                }

                var result = await _messageService.SearchMessagesAsync(q, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages");
                return StatusCode(500, "An error occurred while searching messages.");
            }
        }
    }
}