using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ChatRoom.Api.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ChatRoom.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private static readonly ConcurrentDictionary<string, string> _connectedUsers = new();

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            
            // If we have a username associated with this connection, notify others
            if (_connectedUsers.TryRemove(Context.ConnectionId, out string username))
            {
                _logger.LogInformation($"User {username} left the chat");
                
                // Broadcast a message to notify everyone that a user has left
                await Clients.Others.SendAsync(
                    "message", 
                    new MessageDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        Text = $"{username} has left the chat",
                        Username = "System",
                        Timestamp = DateTime.UtcNow.ToString("o")
                    }
                );
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(object userData)
        {
            try
            {
                var dynamicData = (dynamic)userData;
                string? username = dynamicData?.username;

                if (!string.IsNullOrEmpty(username))
                {
                    // Store username with connection id
                    _connectedUsers[Context.ConnectionId] = username;
                    
                    _logger.LogInformation($"User {username} joined the chat");
                    
                    // Broadcast a message to notify everyone that a new user has joined
                    await Clients.Others.SendAsync(
                        "message", 
                        new MessageDto
                        {
                            Id = Guid.NewGuid().ToString(),
                            Text = $"{username} has joined the chat",
                            Username = "System",
                            Timestamp = DateTime.UtcNow.ToString("o")
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinRoom");
            }
        }

        public async Task SendMessage(object messageData)
        {
            try
            {
                var dynamicMessage = (dynamic)messageData;
                string? text = dynamicMessage?.text;
                string? username = dynamicMessage?.username;

                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(username))
                {
                    return;
                }
                
                // Basic input validation to prevent potential malicious content
                if (text.Length > 1000 || username.Length > 20)
                {
                    _logger.LogWarning($"Rejected message from {username} due to length constraints");
                    return;
                }

                var messageDto = new MessageDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = text,
                    Username = username,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

                // Broadcast the message to all clients
                await Clients.All.SendAsync("message", messageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage");
            }
        }

        public async Task SendTypingStatus(object typingData)
        {
            try
            {
                var dynamicData = (dynamic)typingData;
                string? username = dynamicData?.username;
                bool isTyping = dynamicData?.isTyping ?? false;

                if (string.IsNullOrEmpty(username))
                {
                    return;
                }

                // Broadcast typing status to all clients except the sender
                await Clients.Others.SendAsync("typingStatus", new 
                { 
                    username, 
                    isTyping 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendTypingStatus");
            }
        }
    }
}