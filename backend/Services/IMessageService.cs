using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatRoom.Api.Models;
using ChatRoom.Api.Models.DTOs;

namespace ChatRoom.Api.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageDto>> GetAllMessagesAsync();
        Task<PagedResultDto<MessageDto>> GetMessagesPagedAsync(int page, int pageSize);
        Task<MessageDto> GetMessageByIdAsync(Guid id);
        Task<MessageDto> CreateMessageAsync(string text, string username);
    }
}
