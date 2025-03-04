using System;

namespace ChatRoom.Api.Models.DTOs
{
    public class MessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}
