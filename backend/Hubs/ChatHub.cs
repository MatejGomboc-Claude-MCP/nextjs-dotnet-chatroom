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
        
        // Map connection IDs to user connection info
        private static readonly ConcurrentDictionary<string, UserConnection> _connectedUsers = new();
        // Map username to multiple session IDs
        private static readonly ConcurrentDictionary<string, HashSet<string>> _usernameToSessions = new();
        // Map session ID to connection ID
        private static readonly ConcurrentDictionary<string, string> _sessionToConnectionId = new();
        // HTML encoder for sanitization
        private static readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;
        // Valid emojis for reactions
        private static readonly HashSet<string> _validEmojis = new() { "👍", "❤️", "😂", "😮", "😢", "👏" };
        // Connection timeout (minutes)
        private const int ConnectionTimeoutMinutes = 30;

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
            
            // If we have a user connection associated with this connection, clean up
            if (_connectedUsers.TryRemove(Context.ConnectionId, out var userConnection))
            {
                string username = userConnection.Username;
                string sessionId = userConnection.SessionId;
                
                // Remove session mapping
                _sessionToConnectionId.TryRemove(sessionId, out _);
                
                // Remove from username sessions
                if (_usernameToSessions.TryGetValue(username, out var sessions))
                {
                    // Remove this session from the set
                    sessions.Remove(sessionId);
                    
                    // If no more sessions for this username, remove the username mapping
                    if (sessions.Count == 0)
                    {
                        _usernameToSessions.TryRemove(username, out _);
                        
                        // Send notification that the user has left completely
                        _logger.LogInformation($"User {username} left the chat (all sessions disconnected)");
                        
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
                    else
                    {
                        _logger.LogInformation($"User {username} disconnected one session, but still has {sessions.Count} active sessions");
                    }
                }
                
                // Send updated list of active users
                await BroadcastActiveUsers();
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Cleanup expired connections
        public static async Task CleanupExpiredConnections(IHubContext<ChatHub> hubContext)
        {
            var now = DateTime.UtcNow;
            var expiredConnections = new List<string>();
            var usersToNotify = new HashSet<string>();
            
            // Find expired connections
            foreach (var connection in _connectedUsers)
            {
                if ((now - connection.Value.LastActivity).TotalMinutes > ConnectionTimeoutMinutes)
                {
                    expiredConnections.Add(connection.Key);
                    
                    // Keep track of which usernames we need to check for full disconnection
                    usersToNotify.Add(connection.Value.Username);
                }
            }
            
            // Process expired connections
            foreach (var connectionId in expiredConnections)
            {
                if (_connectedUsers.TryRemove(connectionId, out var userConnection))
                {
                    string username = userConnection.Username;
                    string sessionId = userConnection.SessionId;
                    
                    // Remove session mapping
                    _sessionToConnectionId.TryRemove(sessionId, out _);
                    
                    // Remove from username sessions
                    if (_usernameToSessions.TryGetValue(username, out var sessions))
                    {
                        // Remove this session from the set
                        sessions.Remove(sessionId);
                        
                        // If no more sessions for this username, remove the username mapping
                        if (sessions.Count == 0)
                        {
                            _usernameToSessions.TryRemove(username, out _);
                        }
                    }
                    
                    // Try to disconnect the client
                    try
                    {
                        await hubContext.Clients.Client(connectionId).SendAsync("disconnected", 
                            "Your connection has timed out due to inactivity");
                    }
                    catch (Exception ex)
                    {
                        // Connection might already be closed
                        _logger.LogDebug($"Error sending disconnect message: {ex.Message}");
                    }
                }
            }
            
            // Notify about users who have fully disconnected
            foreach (var username in usersToNotify)
            {
                if (!_usernameToSessions.ContainsKey(username) || _usernameToSessions[username].Count == 0)
                {
                    // This user has no more active sessions, notify everyone
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
                }
            }
            
            // Update active users if any connections were expired
            if (expiredConnections.Count > 0)
            {
                // Get distinct usernames from remaining connections
                var activeUsers = _connectedUsers.Values
                    .Select(u => u.Username)
                    .Distinct()
                    .ToList();
                
                await hubContext.Clients.All.SendAsync("activeUsers", activeUsers);
            }
        }

        public async Task<object> JoinRoom(object userData)
        {
            try
            {
                var dynamicData = (dynamic)userData;
                string? username = dynamicData?.username;
                string? sessionId = dynamicData?.sessionId;

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
                
                // Validate session ID
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = "session_" + Guid.NewGuid().ToString();
                    _logger.LogWarning($"Client {Context.ConnectionId} provided no session ID. Generated: {sessionId}");
                }
                
                // Update activity timestamp if this session is already connected
                if (_sessionToConnectionId.TryGetValue(sessionId, out var existingConnectionId))
                {
                    if (existingConnectionId != Context.ConnectionId)
                    {
                        // Same session ID but different connection - this might be a reconnect or duplicate
                        // Remove the old connection
                        if (_connectedUsers.TryRemove(existingConnectionId, out _))
                        {
                            _logger.LogInformation($"Removed previous connection {existingConnectionId} for session {sessionId}");
                            
                            try
                            {
                                // Tell old connection it's being replaced
                                await Clients.Client(existingConnectionId).SendAsync("disconnected", 
                                    "Your connection was replaced by a new session");
                            }
                            catch
                            {
                                // Ignore errors - connection might already be dead
                            }
                        }
                    }
                }
                
                // Check if username has a different user with that name
                if (_usernameToSessions.TryGetValue(username, out var existingSessions))
                {
                    bool sessionExists = false;
                    
                    // Check if this specific session already exists for this username
                    foreach (var session in existingSessions)
                    {
                        if (session == sessionId)
                        {
                            sessionExists = true;
                            break;
                        }
                    }
                    
                    // If not the same session, this is a new connection for the same username
                    if (!sessionExists)
                    {
                        _logger.LogInformation($"User {username} connected with a new session {sessionId}");
                    }
                }
                else
                {
                    // First time this username is connecting
                    _usernameToSessions[username] = new HashSet<string>();
                    
                    // Broadcast join message only for the first session of this user
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
                
                // Add this session to the username's sessions
                _usernameToSessions[username].Add(sessionId);
                
                // Map session to connection
                _sessionToConnectionId[sessionId] = Context.ConnectionId;
                
                // Create user connection object
                var userConnection = new UserConnection
                {
                    Username = username,
                    SessionId = sessionId,
                    ConnectedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow
                };
                
                // Store connection info
                _connectedUsers[Context.ConnectionId] = userConnection;
                
                _logger.LogInformation($"User {username} joined with session {sessionId} (connection {Context.ConnectionId})");
                
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
                // Update last activity
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
                // Update last activity
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
                // Update last activity
                UpdateUserActivity();
                
                var activeUsers = _connectedUsers.Values
                    .Select(u => u.Username)
                    .Distinct()
                    .ToList();
                
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
                // Update last activity
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
                // Update last activity
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
                // Update last activity
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
                // Update last activity
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
                // Update last activity
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
            var activeUsers = _connectedUsers.Values
                .Select(u => u.Username)
                .Distinct()
                .ToList();
                
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
            return userConnection.Username == username;
        }
    }
}