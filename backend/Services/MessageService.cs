using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatRoom.Api.Data;
using ChatRoom.Api.Models;
using ChatRoom.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.Api.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MessageDto>> GetAllMessagesAsync()
        {
            var messages = await _context.Messages
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return messages.Select(MapToDto);
        }
        
        public async Task<PagedResultDto<MessageDto>> GetMessagesPagedAsync(int page, int pageSize)
        {
            // Ensure valid parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;
            
            // Calculate skip
            int skip = (page - 1) * pageSize;
            
            // Get total count
            int totalCount = await _context.Messages.CountAsync();
            
            // Calculate page count
            int pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Get data for current page
            var messages = await _context.Messages
                .OrderByDescending(m => m.Timestamp)  // Most recent first
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
                
            // Create result
            var result = new PagedResultDto<MessageDto>
            {
                Items = messages.Select(MapToDto),
                TotalCount = totalCount,
                PageCount = pageCount,
                CurrentPage = page,
                PageSize = pageSize,
                HasNext = page < pageCount,
                HasPrevious = page > 1
            };
            
            return result;
        }

        public async Task<MessageDto> GetMessageByIdAsync(Guid id)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
                throw new KeyNotFoundException($"Message with ID {id} not found.");

            return MapToDto(message);
        }

        public async Task<MessageDto> CreateMessageAsync(string text, string username)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Message text cannot be empty.", nameof(text));

            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));

            var message = new Message
            {
                Id = Guid.NewGuid(),
                Text = text,
                Username = username,
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return MapToDto(message);
        }

        private static MessageDto MapToDto(Message message)
        {
            return new MessageDto
            {
                Id = message.Id.ToString(),
                Text = message.Text,
                Username = message.Username,
                Timestamp = message.Timestamp.ToString("o") // ISO 8601 format
            };
        }
    }
}
