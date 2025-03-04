using System;
using System.Threading.Tasks;
using ChatRoom.Api.Data;
using ChatRoom.Api.Models;
using ChatRoom.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChatRoom.Api.Tests
{
    public class MessageServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

        public MessageServiceTests()
        {
            // Use in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
        }

        [Fact]
        public async Task CreateMessageAsync_ValidInput_ReturnsMessageDto()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = new MessageService(context);
            var text = "Test message";
            var username = "testuser";

            // Act
            var result = await service.CreateMessageAsync(text, username);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(text, result.Text);
            Assert.Equal(username, result.Username);
            Assert.NotEmpty(result.Id);
            Assert.NotEmpty(result.Timestamp);
        }

        [Fact]
        public async Task CreateMessageAsync_EmptyText_ThrowsArgumentException()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = new MessageService(context);
            var text = "";
            var username = "testuser";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateMessageAsync(text, username));
        }

        [Fact]
        public async Task CreateMessageAsync_EmptyUsername_ThrowsArgumentException()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = new MessageService(context);
            var text = "Test message";
            var username = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateMessageAsync(text, username));
        }

        [Fact]
        public async Task GetMessagesPagedAsync_ReturnsCorrectPageSize()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = new MessageService(context);
            
            // Add some test messages
            for (int i = 0; i < 25; i++)
            {
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    Text = $"Test message {i}",
                    Username = "testuser",
                    Timestamp = DateTime.UtcNow
                };
                await context.Messages.AddAsync(message);
            }
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetMessagesPagedAsync(1, 10);

            // Assert
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(3, result.PageCount);
            Assert.Equal(1, result.CurrentPage);
            Assert.True(result.HasNext);
            Assert.False(result.HasPrevious);
        }

        [Fact]
        public async Task GetMessageByIdAsync_ExistingId_ReturnsMessage()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = new MessageService(context);
            
            var messageId = Guid.NewGuid();
            var message = new Message
            {
                Id = messageId,
                Text = "Test message",
                Username = "testuser",
                Timestamp = DateTime.UtcNow
            };
            await context.Messages.AddAsync(message);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetMessageByIdAsync(messageId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(messageId.ToString(), result.Id);
            Assert.Equal("Test message", result.Text);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public async Task GetMessageByIdAsync_NonExistingId_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = new MessageService(context);
            var messageId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetMessageByIdAsync(messageId));
        }
    }
}