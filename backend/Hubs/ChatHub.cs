using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatRoom.Api.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Web;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using ChatRoom.Api.Services;

namespace ChatRoom.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IReactionService _reactionService;
        
        // Map connection IDs to usernames and connection time
        private static readonly ConcurrentDictionary<string, UserConnection> _connectedUsers = new();
        // Reverse mapping to easily check for duplicates (username -> connectionId)
        private static readonly ConcurrentDictionary<string, string> _usernameToConnectionId = new();
        // HTML encoder for sanitization
        private static readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;
        // Valid emojis for reactions
        private static readonly HashSet<string> _validEmojis = new() { "👍", "❤️", "😂", "😮", "😢", "👏" };
        // Connection timeout (minutes)
        private const int ConnectionTimeoutMinutes = 30;

        // Track user connection with timestamp
        private class UserConnection
        {
            public string Username { get; set; } = string.Empty;
            public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
            public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        }

        public ChatHub(ILogger<ChatHub> logger, IReactionService reactionService)
        {
            _logger = logger;
            _reactionService = reactionService;
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
            if (_connectedUsers.TryRemove(Context.ConnectionId, out var userConnection))
            {
                string username = userConnection.Username;
                // Try to remove from reverse mapping only if this connection owns the username
                if (_usernameToConnectionId.TryGetValue(username, out string currentConnectionId) && 
                    currentConnectionId == Context.ConnectionId)
                {
                    _usernameToConnectionId.TryRemove(username, out _);
                    
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

                    // Send updated list of active users
                    await BroadcastActiveUsers();
                }
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Cleanup expired connections
        public static async Task CleanupExpiredConnections(IHubContext<ChatHub> hubContext)
        {
            var now = DateTime.UtcNow;
            var expiredConnections = new List<string>();
            
            // Find expired connections
            foreach (var connection in _connectedUsers)
            {
                if ((now - connection.Value.LastActivity).TotalMinutes > ConnectionTimeoutMinutes)
                {
                    expiredConnections.Add(connection.Key);
                }
            }
            
            // Process expired connections
            foreach (var connectionId in expiredConnections)
            {
                if (_connectedUsers.TryRemove(connectionId, out var userConnection))
                {
                    string username = userConnection.Username;
                    
                    // Only remove the username mapping if it still points to this connection
                    if (_usernameToConnectionId.TryGetValue(username, out string currentConnectionId) && 
                        currentConnectionId == connectionId)
                    {
                        _usernameToConnectionId.TryRemove(username, out _);
                        
                        // Notify clients that this user has left
                        await hubContext.Clients.All.SendAsync(
                            "message",
                            new MessageDto
                            {
                                Id = Guid.NewGuid().ToString(),
                                Text = $"{username} has left the chat (connection timeout)",
                                Username = "System",
                                Timestamp = DateTime.UtcNow.ToString("o")
                            }
                        );
                        
                        // Update active users list
                        await hubContext.Clients.All.SendAsync("activeUsers", 
                            _connectedUsers.Values.Select(u => u.Username).Distinct().ToList());
                    }
                    
                    // Try to disconnect the client
                    try
                    {
                        await hubContext.Clients.Client(connectionId).SendAsync("disconnected", 
                            "Your connection has timed out due to inactivity");
                    }
                    catch (Exception)
                    {
                        // Connection might already be closed
                    }
                }
            }
        }

        public async Task<object> JoinRoom(object userData)
        {
            try
            {
                var dynamicData = (dynamic)userData;
                string? username = dynamicData?.username;

                // Validate username
                if (string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Username cannot be empty" };
                }

                if (username.Length < 3 || username.Length > 20)
                {
                    return new { success = false, error = "Username must be between 3 and 20 characters" };
                }

                // Use regex to validate username (alphanumeric and underscore only)
                if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
                {
                    return new { success = false, error = "Username can only contain letters, numbers, and underscores" };
                }

                // Check if username is already in use by another connection
                if (_usernameToConnectionId.TryGetValue(username, out string existingConnectionId))
                {
                    if (existingConnectionId != Context.ConnectionId)
                    {
                        // If the existing connection is still active and not expired
                        if (_connectedUsers.TryGetValue(existingConnectionId, out var existingConnection))
                        {
                            // Check if the connection has been inactive for more than 5 minutes
                            if ((DateTime.UtcNow - existingConnection.LastActivity).TotalMinutes < 5)
                            {
                                return new { success = false, error = "Username is already in use" };
                            }
                            
                            // If inactive, take over the username
                            _connectedUsers.TryRemove(existingConnectionId, out _);
                            _logger.LogInformation($"Inactive connection {existingConnectionId} for username {username} has been replaced");
                        }
                    }
                }

                // Handle case where user is reconnecting with same username
                // Remove any previous connection with this username
                foreach (var conn in _connectedUsers.Where(c => c.Value.Username == username).ToList())
                {
                    if (conn.Key != Context.ConnectionId)
                    {
                        _connectedUsers.TryRemove(conn.Key, out _);
                        _logger.LogInformation($"Removed old connection {conn.Key} for username {username}");
                    }
                }

                // Update or create user connection
                var userConnection = new UserConnection
                {
                    Username = username,
                    ConnectedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow
                };
                
                // Store username with connection id (both ways for easy lookup)
                _connectedUsers[Context.ConnectionId] = userConnection;
                _usernameToConnectionId[username] = Context.ConnectionId;
                
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

                // Send updated list of active users
                await BroadcastActiveUsers();
                
                return new { success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinRoom");
                return new { success = false, error = "An error occurred while joining the chat" };
            }
        }

        public async Task<object> SendMessage(object messageData)
        {
            try
            {
                UpdateUserActivity();
                
                var dynamicMessage = (dynamic)messageData;
                string? text = dynamicMessage?.text;
                string? username = dynamicMessage?.username;

                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message text and username are required" };
                }
                
                // Basic input validation to prevent potential malicious content
                if (text.Length > 1000 || username.Length > 20)
                {
                    _logger.LogWarning($"Rejected message from {username} due to length constraints");
                    return new { success = false, error = "Message or username exceeds maximum length" };
                }

                // Verify user is connected
                if (!VerifyUserConnection(username))
                {
                    _logger.LogWarning($"Rejected message from unauthorized user: {username}");
                    return new { success = false, error = "Unauthorized to send messages" };
                }

                // Sanitize text to prevent XSS
                string sanitizedText = SanitizeInput(text);
                
                var messageId = Guid.NewGuid().ToString();
                var messageDto = new MessageDto
                {
                    Id = messageId,
                    Text = sanitizedText,
                    Username = username,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

                // Broadcast the message to all clients
                await Clients.All.SendAsync("message", messageDto);
                return new { success = true, messageId = messageDto.Id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage");
                return new { success = false, error = "An error occurred while sending the message" };
            }
        }

        public async Task SendTypingStatus(object typingData)
        {
            try
            {
                UpdateUserActivity();
                
                var dynamicData = (dynamic)typingData;
                string? username = dynamicData?.username;
                bool isTyping = dynamicData?.isTyping ?? false;

                if (string.IsNullOrEmpty(username))
                {
                    return;
                }

                // Verify user is connected with the claimed username
                if (!VerifyUserConnection(username))
                {
                    _logger.LogWarning($"Rejected typing status from unauthorized user: {username}");
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

        // Get all active users
        public async Task<object> GetActiveUsers()
        {
            try
            {
                UpdateUserActivity();
                
                var activeUsers = _connectedUsers.Values.Select(u => u.Username).Distinct().ToList();
                return new { success = true, users = activeUsers };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActiveUsers");
                return new { success = false, error = "Failed to retrieve active users" };
            }
        }

        // Delete a message (for moderators or message owners)
        public async Task<object> DeleteMessage(object messageData)
        {
            try
            {
                UpdateUserActivity();
                
                var dynamicData = (dynamic)messageData;
                string? messageId = dynamicData?.messageId;
                string? username = dynamicData?.username;

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message ID and username are required" };
                }

                // Verify user is connected
                if (!VerifyUserConnection(username))
                {
                    _logger.LogWarning($"Rejected message deletion from unauthorized user: {username}");
                    return new { success = false, error = "Unauthorized to delete messages" };
                }

                // In a real app, we'd verify message ownership in the database
                // For now, we'll just broadcast the deletion event
                
                await Clients.All.SendAsync("messageDeleted", new { messageId });
                return new { success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteMessage");
                return new { success = false, error = "An error occurred while deleting the message" };
            }
        }

        // Edit a message
        public async Task<object> EditMessage(object messageData)
        {
            try
            {
                UpdateUserActivity();
                
                var dynamicData = (dynamic)messageData;
                string? messageId = dynamicData?.messageId;
                string? newText = dynamicData?.text;
                string? username = dynamicData?.username;

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(newText) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message ID, text, and username are required" };
                }

                // Verify user is connected
                if (!VerifyUserConnection(username))
                {
                    _logger.LogWarning($"Rejected message edit from unauthorized user: {username}");
                    return new { success = false, error = "Unauthorized to edit messages" };
                }

                // Basic validation
                if (newText.Length > 1000)
                {
                    return new { success = false, error = "Message exceeds maximum length" };
                }

                // Sanitize text to prevent XSS
                string sanitizedText = SanitizeInput(newText);

                // In a real app, we'd verify message ownership in the database
                // For now, we'll just broadcast the edit event
                
                await Clients.All.SendAsync("messageEdited", new 
                { 
                    messageId, 
                    text = sanitizedText,
                    editedAt = DateTime.UtcNow.ToString("o")
                });
                
                return new { success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditMessage");
                return new { success = false, error = "An error occurred while editing the message" };
            }
        }

        // Add a reaction to a message
        public async Task<object> AddReaction(object reactionData)
        {
            try
            {
                UpdateUserActivity();
                
                var dynamicData = (dynamic)reactionData;
                string? messageId = dynamicData?.messageId;
                string? emoji = dynamicData?.emoji;
                string? username = dynamicData?.username;

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(emoji) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message ID, emoji, and username are required" };
                }

                // Verify user is connected
                if (!VerifyUserConnection(username))
                {
                    _logger.LogWarning($"Rejected reaction from unauthorized user: {username}");
                    return new { success = false, error = "Unauthorized to add reactions" };
                }

                // Validate emoji
                if (!_validEmojis.Contains(emoji))
                {
                    return new { success = false, error = "Invalid emoji" };
                }

                // Add reaction to database through the service
                try
                {
                    Guid messageGuid = Guid.Parse(messageId);
                    bool success = await _reactionService.AddReactionAsync(messageGuid, emoji, username);
                    
                    if (success)
                    {
                        // Get updated reactions
                        var reactions = await _reactionService.GetReactionsForMessageAsync(messageGuid);
                        
                        // Broadcast the updated reactions
                        await Clients.All.SendAsync("messageReactions", new
                        {
                            messageId,
                            reactions
                        });
                        
                        return new { success = true };
                    }
                    else
                    {
                        return new { success = false, error = "Failed to add reaction" };
                    }
                }
                catch (FormatException)
                {
                    return new { success = false, error = "Invalid message ID format" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddReaction");
                return new { success = false, error = "An error occurred while adding the reaction" };
            }
        }

        // Remove a reaction from a message
        public async Task<object> RemoveReaction(object reactionData)
        {
            try
            {
                UpdateUserActivity();
                
                var dynamicData = (dynamic)reactionData;
                string? messageId = dynamicData?.messageId;
                string? emoji = dynamicData?.emoji;
                string? username = dynamicData?.username;

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(emoji) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message ID, emoji, and username are required" };
                }

                // Verify user is connected
                if (!VerifyUserConnection(username))
                {
                    _logger.LogWarning($"Rejected reaction removal from unauthorized user: {username}");
                    return new { success = false, error = "Unauthorized to remove reactions" };
                }

                // Remove reaction from database through the service
                try
                {
                    Guid messageGuid = Guid.Parse(messageId);
                    bool success = await _reactionService.RemoveReactionAsync(messageGuid, emoji, username);
                    
                    if (success)
                    {
                        // Get updated reactions
                        var reactions = await _reactionService.GetReactionsForMessageAsync(messageGuid);
                        
                        // Broadcast the updated reactions
                        await Clients.All.SendAsync("messageReactions", new
                        {
                            messageId,
                            reactions
                        });
                        
                        return new { success = true };
                    }
                    else
                    {
                        return new { success = false, error = "Failed to remove reaction" };
                    }
                }
                catch (FormatException)
                {
                    return new { success = false, error = "Invalid message ID format" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveReaction");
                return new { success = false, error = "An error occurred while removing the reaction" };
            }
        }

        // Get reactions for a message
        public async Task<object> GetReactions(object messageData)
        {
            try
            {
                UpdateUserActivity();
                
                var dynamicData = (dynamic)messageData;
                string? messageId = dynamicData?.messageId;

                if (string.IsNullOrEmpty(messageId))
                {
                    return new { success = false, error = "Message ID is required" };
                }

                try
                {
                    Guid messageGuid = Guid.Parse(messageId);
                    var reactions = await _reactionService.GetReactionsForMessageAsync(messageGuid);
                    
                    return new { success = true, reactions };
                }
                catch (FormatException)
                {
                    return new { success = false, error = "Invalid message ID format" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReactions");
                return new { success = false, error = "An error occurred while getting reactions" };
            }
        }

        // Broadcast active users to all clients
        private async Task BroadcastActiveUsers()
        {
            var activeUsers = _connectedUsers.Values.Select(u => u.Username).Distinct().ToList();
            await Clients.All.SendAsync("activeUsers", activeUsers);
        }

        // Sanitize input to prevent XSS
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // HTML encode the input to prevent XSS
            return _htmlEncoder.Encode(input);
        }
        
        // Update the last activity timestamp for the current user
        private void UpdateUserActivity()
        {
            if (_connectedUsers.TryGetValue(Context.ConnectionId, out var userConnection))
            {
                userConnection.LastActivity = DateTime.UtcNow;
            }
        }
        
        // Verify that the user is connected and authorized to perform actions
        private bool VerifyUserConnection(string username)
        {
            // Check if connection ID exists in our tracking
            if (!_connectedUsers.TryGetValue(Context.ConnectionId, out var userConnection))
            {
                return false;
            }
            
            // Check if the username matches
            if (userConnection.Username != username)
            {
                return false;
            }
            
            // Check if this connection is the current owner of the username
            if (!_usernameToConnectionId.TryGetValue(username, out string currentConnectionId) ||
                currentConnectionId != Context.ConnectionId)
            {
                return false;
            }
            
            return true;
        }
    }
}