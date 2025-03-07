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

namespace ChatRoom.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        // Map connection IDs to usernames
        private static readonly ConcurrentDictionary<string, string> _connectedUsers = new();
        // Reverse mapping to easily check for duplicates
        private static readonly ConcurrentDictionary<string, string> _usernameToConnectionId = new();
        // Message reactions storage - messageId -> emoji -> list of usernames
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<string>>> _messageReactions = new();
        // HTML encoder for sanitization
        private static readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;
        // Valid emojis for reactions
        private static readonly HashSet<string> _validEmojis = new() { "👍", "❤️", "😂", "😮", "😢", "👏" };

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
                // Remove from reverse mapping
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
            
            await base.OnDisconnectedAsync(exception);
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

                // Check if username is already in use
                if (_usernameToConnectionId.TryGetValue(username, out string existingConnectionId))
                {
                    if (existingConnectionId != Context.ConnectionId)
                    {
                        return new { success = false, error = "Username is already in use" };
                    }
                }

                // Store username with connection id (both ways for easy lookup)
                _connectedUsers[Context.ConnectionId] = username;
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
                if (!_connectedUsers.TryGetValue(Context.ConnectionId, out string storedUsername) || storedUsername != username)
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

                // Initialize reactions dictionary for this message
                _messageReactions[messageId] = new ConcurrentDictionary<string, List<string>>();
                foreach (var emoji in _validEmojis)
                {
                    _messageReactions[messageId][emoji] = new List<string>();
                }

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
                var dynamicData = (dynamic)typingData;
                string? username = dynamicData?.username;
                bool isTyping = dynamicData?.isTyping ?? false;

                if (string.IsNullOrEmpty(username))
                {
                    return;
                }

                // Verify user is connected with the claimed username
                if (!_connectedUsers.TryGetValue(Context.ConnectionId, out string storedUsername) || storedUsername != username)
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
                var activeUsers = _connectedUsers.Values.Distinct().ToList();
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
                var dynamicData = (dynamic)messageData;
                string? messageId = dynamicData?.messageId;
                string? username = dynamicData?.username;

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message ID and username are required" };
                }

                // Verify user is connected with the claimed username
                if (!_connectedUsers.TryGetValue(Context.ConnectionId, out string storedUsername) || storedUsername != username)
                {
                    _logger.LogWarning($"Rejected message deletion from unauthorized user: {username}");
                    return new { success = false, error = "Unauthorized to delete messages" };
                }

                // In a real app, we'd verify message ownership in the database
                // For now, we'll just broadcast the deletion event
                
                // Remove reactions for this message
                _messageReactions.TryRemove(messageId, out _);
                
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
                var dynamicData = (dynamic)messageData;
                string? messageId = dynamicData?.messageId;
                string? newText = dynamicData?.text;
                string? username = dynamicData?.username;

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(newText) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message ID, text, and username are required" };
                }

                // Verify user is connected with the claimed username
                if (!_connectedUsers.TryGetValue(Context.ConnectionId, out string storedUsername) || storedUsername != username)
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
                var dynamicData = (dynamic)reactionData;
                string? messageId = dynamicData?.messageId;
                string? emoji = dynamicData?.emoji;
                string? username = dynamicData?.username;

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(emoji) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message ID, emoji, and username are required" };
                }

                // Verify user is connected
                if (!_connectedUsers.TryGetValue(Context.ConnectionId, out string storedUsername) || storedUsername != username)
                {
                    _logger.LogWarning($"Rejected reaction from unauthorized user: {username}");
                    return new { success = false, error = "Unauthorized to add reactions" };
                }

                // Validate emoji
                if (!_validEmojis.Contains(emoji))
                {
                    return new { success = false, error = "Invalid emoji" };
                }

                // Check if message exists
                if (!_messageReactions.TryGetValue(messageId, out var messageEmojis))
                {
                    _messageReactions[messageId] = new ConcurrentDictionary<string, List<string>>();
                    foreach (var validEmoji in _validEmojis)
                    {
                        _messageReactions[messageId][validEmoji] = new List<string>();
                    }
                    messageEmojis = _messageReactions[messageId];
                }

                // Add user to the reaction if not already there
                if (!messageEmojis.TryGetValue(emoji, out var users))
                {
                    messageEmojis[emoji] = new List<string>() { username };
                }
                else if (!users.Contains(username))
                {
                    users.Add(username);
                }

                // Broadcast updated reactions
                await BroadcastReactions(messageId);
                
                return new { success = true };
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
                var dynamicData = (dynamic)reactionData;
                string? messageId = dynamicData?.messageId;
                string? emoji = dynamicData?.emoji;
                string? username = dynamicData?.username;

                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(emoji) || string.IsNullOrEmpty(username))
                {
                    return new { success = false, error = "Message ID, emoji, and username are required" };
                }

                // Verify user is connected
                if (!_connectedUsers.TryGetValue(Context.ConnectionId, out string storedUsername) || storedUsername != username)
                {
                    _logger.LogWarning($"Rejected reaction removal from unauthorized user: {username}");
                    return new { success = false, error = "Unauthorized to remove reactions" };
                }

                // Check if message and emoji exist
                if (_messageReactions.TryGetValue(messageId, out var messageEmojis) && 
                    messageEmojis.TryGetValue(emoji, out var users))
                {
                    users.Remove(username);
                }

                // Broadcast updated reactions
                await BroadcastReactions(messageId);
                
                return new { success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveReaction");
                return new { success = false, error = "An error occurred while removing the reaction" };
            }
        }

        // Get reactions for a message
        public object GetReactions(object messageData)
        {
            try
            {
                var dynamicData = (dynamic)messageData;
                string? messageId = dynamicData?.messageId;

                if (string.IsNullOrEmpty(messageId))
                {
                    return new { success = false, error = "Message ID is required" };
                }

                if (_messageReactions.TryGetValue(messageId, out var reactions))
                {
                    // Convert to dictionary of emoji -> { count, usernames }
                    var formattedReactions = reactions.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new { count = kvp.Value.Count, usernames = kvp.Value }
                    );
                    
                    return new { success = true, reactions = formattedReactions };
                }

                return new { success = true, reactions = new Dictionary<string, object>() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReactions");
                return new { success = false, error = "An error occurred while getting reactions" };
            }
        }

        // Broadcast reactions to all clients
        private async Task BroadcastReactions(string messageId)
        {
            if (_messageReactions.TryGetValue(messageId, out var reactions))
            {
                // Convert to dictionary of emoji -> { count, usernames }
                var formattedReactions = reactions.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new { count = kvp.Value.Count, usernames = kvp.Value }
                );
                
                await Clients.All.SendAsync("messageReactions", new
                {
                    messageId,
                    reactions = formattedReactions
                });
            }
        }

        // Broadcast active users to all clients
        private async Task BroadcastActiveUsers()
        {
            var activeUsers = _connectedUsers.Values.Distinct().ToList();
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
    }
}