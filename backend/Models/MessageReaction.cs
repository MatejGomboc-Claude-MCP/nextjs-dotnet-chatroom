using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatRoom.Api.Models
{
    public class MessageReaction
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public string Emoji { get; set; } = string.Empty;
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [Required]
        public Guid MessageId { get; set; }
        
        [ForeignKey("MessageId")]
        public virtual Message Message { get; set; } = null!;
    }
}