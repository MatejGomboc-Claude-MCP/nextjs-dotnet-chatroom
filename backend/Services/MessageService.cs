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
        
        public async Task<MessageDto> UpdateMessageAsync(Guid id, string text, string username)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Message text cannot be empty.", nameof(text));

            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));

            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
            
            if (message == null)
                throw new KeyNotFoundException($"Message with ID {id} not found.");
                
            // Check if the user is the message author (in a real app, you might have more complex authorization)
            if (message.Username != username)
                throw new UnauthorizedAccessException($"User {username} is not authorized to edit this message.");
            
            // Update message
            message.Text = text;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return MapToDto(message);
        }
        
        public async Task<bool> DeleteMessageAsync(Guid id, string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));
                
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
            
            if (message == null)
                return false; // Message already deleted or never existed
                
            // Check if the user is the message author (in a real app, you might have more complex authorization)
            if (message.Username != username)
                throw new UnauthorizedAccessException($"User {username} is not authorized to delete this message.");
            
            // Delete message
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        public async Task<PagedResultDto<MessageDto>> SearchMessagesAsync(string query, int page, int pageSize)
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("Search query cannot be empty.", nameof(query));
                
            // Ensure valid parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;
            
            // Calculate skip
            int skip = (page - 1) * pageSize;
            
            // Normalize query to lowercase for case-insensitive search
            var normalizedQuery = query.ToLower();
            
            // Get filtered and paginated data
            var messagesQuery = _context.Messages
                .Where(m => m.Text.ToLower().Contains(normalizedQuery) || 
                           m.Username.ToLower().Contains(normalizedQuery))
                .OrderByDescending(m => m.Timestamp);
                
            // Get total count of filtered items
            int totalCount = await messagesQuery.CountAsync();
            
            // Calculate page count
            int pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Get the page of data
            var messages = await messagesQuery
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

        private static MessageDto MapToDto(Message message)
        {
            return new MessageDto
            {
                Id = message.Id.ToString(),
                Text = message.Text,
                Username = message.Username,
                Timestamp = message.Timestamp.ToString("o"), // ISO 8601 format
                IsEdited = message.IsEdited,
                EditedAt = message.EditedAt?.ToString("o")
            };
        }
    }
}