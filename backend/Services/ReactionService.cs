using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatRoom.Api.Data;
using ChatRoom.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatRoom.Api.Services
{
    public class ReactionService : IReactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReactionService> _logger;
        // Set of valid emojis that can be used for reactions
        private static readonly HashSet<string> _validEmojis = new() { "👍", "❤️", "😂", "😮", "😢", "👏" };

        public ReactionService(ApplicationDbContext context, ILogger<ReactionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AddReactionAsync(Guid messageId, string emoji, string username)
        {
            if (string.IsNullOrEmpty(emoji) || string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Emoji and username are required");
            }

            // Validate emoji
            if (!_validEmojis.Contains(emoji))
            {
                throw new ArgumentException($"Invalid emoji: {emoji}");
            }

            try
            {
                // Check if message exists
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    _logger.LogWarning($"Attempted to add reaction to non-existent message: {messageId}");
                    return false;
                }

                // Check if the user already has this reaction
                var existingReaction = await _context.MessageReactions
                    .FirstOrDefaultAsync(r => r.MessageId == messageId && r.Username == username && r.Emoji == emoji);

                // If no existing reaction, add a new one
                if (existingReaction == null)
                {
                    _context.MessageReactions.Add(new MessageReaction
                    {
                        Id = Guid.NewGuid(),
                        MessageId = messageId,
                        Username = username,
                        Emoji = emoji,
                        Timestamp = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding reaction {emoji} for user {username} to message {messageId}");
                throw;
            }
        }

        public async Task<bool> RemoveReactionAsync(Guid messageId, string emoji, string username)
        {
            if (string.IsNullOrEmpty(emoji) || string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Emoji and username are required");
            }

            try
            {
                // Find the reaction to remove
                var reaction = await _context.MessageReactions
                    .FirstOrDefaultAsync(r => r.MessageId == messageId && r.Username == username && r.Emoji == emoji);

                // If reaction exists, remove it
                if (reaction != null)
                {
                    _context.MessageReactions.Remove(reaction);
                    await _context.SaveChangesAsync();
                    return true;
                }

                // If reaction doesn't exist, consider it successful (idempotent operation)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing reaction {emoji} for user {username} from message {messageId}");
                throw;
            }
        }

        public async Task<Dictionary<string, ReactionDto>> GetReactionsForMessageAsync(Guid messageId)
        {
            try
            {
                // Query all reactions for this message
                var reactions = await _context.MessageReactions
                    .Where(r => r.MessageId == messageId)
                    .ToListAsync();

                // Group reactions by emoji
                var result = new Dictionary<string, ReactionDto>();

                // Initialize with all valid emojis (even those with zero reactions)
                foreach (var emoji in _validEmojis)
                {
                    result[emoji] = new ReactionDto
                    {
                        Count = 0,
                        Usernames = new List<string>()
                    };
                }

                // Add actual reactions
                foreach (var reaction in reactions)
                {
                    if (result.TryGetValue(reaction.Emoji, out var dto))
                    {
                        dto.Count++;
                        dto.Usernames.Add(reaction.Username);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving reactions for message {messageId}");
                throw;
            }
        }

        public async Task<Dictionary<string, Dictionary<string, ReactionDto>>> GetReactionsForMessagesAsync(IEnumerable<Guid> messageIds)
        {
            try
            {
                // Convert IEnumerable to list for multiple use
                var messageIdsList = messageIds.ToList();

                // Query all reactions for these messages
                var reactions = await _context.MessageReactions
                    .Where(r => messageIdsList.Contains(r.MessageId))
                    .ToListAsync();

                // Group reactions by message ID and then by emoji
                var result = new Dictionary<string, Dictionary<string, ReactionDto>>();

                // Initialize result for each message ID
                foreach (var messageId in messageIdsList)
                {
                    var messageReactions = new Dictionary<string, ReactionDto>();

                    // Initialize with all valid emojis (even those with zero reactions)
                    foreach (var emoji in _validEmojis)
                    {
                        messageReactions[emoji] = new ReactionDto
                        {
                            Count = 0,
                            Usernames = new List<string>()
                        };
                    }

                    result[messageId.ToString()] = messageReactions;
                }

                // Add actual reactions
                foreach (var reaction in reactions)
                {
                    var messageIdStr = reaction.MessageId.ToString();
                    if (result.TryGetValue(messageIdStr, out var messageReactions) &&
                        messageReactions.TryGetValue(reaction.Emoji, out var dto))
                    {
                        dto.Count++;
                        dto.Usernames.Add(reaction.Username);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reactions for multiple messages");
                throw;
            }
        }
    }
}