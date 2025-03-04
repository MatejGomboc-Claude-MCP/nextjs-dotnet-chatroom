using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoom.Api.Models.DTOs
{
    public class MessageDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Message text must be between 1 and 1000 characters")]
        public string Text { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 50 characters")]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Timestamp { get; set; } = string.Empty;
    }

    public class CreateMessageDto
    {
        [Required]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Message text must be between 1 and 1000 characters")]
        public string Text { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 50 characters")]
        public string Username { get; set; } = string.Empty;
    }
}
