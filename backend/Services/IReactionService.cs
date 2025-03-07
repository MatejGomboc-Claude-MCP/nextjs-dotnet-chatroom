using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatRoom.Api.Models.DTOs;

namespace ChatRoom.Api.Services
{
    public interface IReactionService
    {
        Task<bool> AddReactionAsync(Guid messageId, string emoji, string username);
        Task<bool> RemoveReactionAsync(Guid messageId, string emoji, string username);
        Task<Dictionary<string, ReactionDto>> GetReactionsForMessageAsync(Guid messageId);
        Task<Dictionary<string, Dictionary<string, ReactionDto>>> GetReactionsForMessagesAsync(IEnumerable<Guid> messageIds);
    }

    public class ReactionDto
    {
        public int Count { get; set; }
        public List<string> Usernames { get; set; } = new List<string>();
    }
}