using System;

namespace ChatRoom.Api.Hubs
{
    // Class to track user connection information 
    public class UserConnection
    {
        public string Username { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}