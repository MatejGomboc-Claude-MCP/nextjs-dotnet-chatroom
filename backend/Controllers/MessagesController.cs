using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatRoom.Api.Models.DTOs;
using ChatRoom.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ChatRoom.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        // GET: api/messages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages()
        {
            try
            {
                var messages = await _messageService.GetAllMessagesAsync();
                return Ok(messages);
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
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving message with ID {id}");
                return StatusCode(500, "An error occurred while retrieving the message.");
            }
        }

        // POST: api/messages
        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage([FromBody] CreateMessageDto createMessageDto)
        {
            if (createMessageDto == null)
                return BadRequest("Message data is required.");

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
    }

    public class CreateMessageDto
    {
        public string Text { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}
