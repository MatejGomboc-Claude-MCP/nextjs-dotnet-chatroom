using System;
using System.ComponentModel.DataAnnotations;

namespace ChatRoom.Api.Models
{
    public class Message
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
