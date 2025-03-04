# Testing Strategy

This document outlines the testing approach for the chatroom application.

## Testing Levels

The application employs multiple testing levels to ensure quality:

1. **Unit Testing**: Testing individual components in isolation
2. **Integration Testing**: Testing interactions between components
3. **End-to-End Testing**: Testing complete user workflows
4. **Performance Testing**: Testing system performance under load
5. **Security Testing**: Testing for vulnerabilities and security issues

## Frontend Testing

### Unit Testing

The frontend uses Jest with React Testing Library for unit tests.

**Test Coverage Goals:**
- Components: 80%+
- Services: 90%+
- Utilities: 95%+

**Key Areas to Test:**
- Component rendering
- User interactions (clicks, inputs)
- State management
- Error handling
- Data display
- Conditional rendering

**Example Test Structure:**
```typescript
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import MessageItem from './MessageItem';

describe('MessageItem', () => {
  test('renders message content correctly', () => {
    // Arrange
    const message = {
      id: '1',
      text: 'Hello world',
      username: 'testuser',
      timestamp: '2023-01-01T12:00:00Z',
      isCurrentUser: false
    };
    
    // Act
    render(<MessageItem message={message} />);
    
    // Assert
    expect(screen.getByText('Hello world')).toBeInTheDocument();
    expect(screen.getByText('testuser')).toBeInTheDocument();
  });
});
```

### Integration Testing

Integration tests focus on interactions between components using Jest and React Testing Library.

**Key Integration Points:**
- SignalR service with chat components
- API service with data display components
- Form submissions and error handling

**Example Integration Test:**
```typescript
import { render, screen, act, waitFor } from '@testing-library/react';
import { ChatRoom } from './ChatRoom';
import { chatConnection } from '../services/signalr';
import { messagesApi } from '../services/api';

// Mock dependencies
jest.mock('../services/signalr');
jest.mock('../services/api');

describe('ChatRoom integration', () => {
  test('loads messages and handles new message', async () => {
    // Mock API response
    messagesApi.getMessagesPaged.mockResolvedValue({
      items: [{ id: '1', text: 'Hello', username: 'user1', timestamp: '2023-01-01T12:00:00Z' }],
      totalCount: 1,
      currentPage: 1,
      pageSize: 50,
      pageCount: 1,
      hasNext: false,
      hasPrevious: false
    });
    
    // Render component
    render(<ChatRoom />);
    
    // Verify initial message loaded
    await waitFor(() => {
      expect(screen.getByText('Hello')).toBeInTheDocument();
    });
    
    // Simulate new message from SignalR
    await act(async () => {
      const messageCallback = chatConnection.onMessage.mock.calls[0][0];
      messageCallback({
        id: '2',
        text: 'New message',
        username: 'user2',
        timestamp: '2023-01-01T12:01:00Z'
      });
    });
    
    // Verify new message displayed
    expect(screen.getByText('New message')).toBeInTheDocument();
  });
});
```

### End-to-End Testing

End-to-end testing ensures the entire application works correctly from a user perspective. We use Cypress for E2E tests.

**Key User Flows to Test:**
- User login
- Sending and receiving messages
- Pagination of message history
- Typing indicators
- Handling connection issues

## Backend Testing

### Unit Testing

The backend uses xUnit for unit testing with an in-memory database for data access tests.

**Test Coverage Goals:**
- Controllers: 85%+
- Services: 90%+
- Hubs: 85%+
- Middleware: 90%+

**Example Test Structure:**
```csharp
using System;
using System.Threading.Tasks;
using ChatRoom.Api.Data;
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
            
            // Act
            var result = await service.CreateMessageAsync("Test message", "testuser");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test message", result.Text);
            Assert.Equal("testuser", result.Username);
        }
    }
}
```

### Integration Testing

Integration tests focus on interactions between components and external systems.

**Key Integration Points:**
- API controllers with services
- Services with database
- SignalR hubs with services

**Example Integration Test:**
```csharp
[Fact]
public async Task GetMessages_ReturnsCorrectPagination()
{
    // Arrange
    using var context = new ApplicationDbContext(_dbContextOptions);
    
    // Add test data
    for (int i = 0; i < 25; i++)
    {
        await context.Messages.AddAsync(new Message 
        { 
            Id = Guid.NewGuid(), 
            Text = $"Message {i}", 
            Username = "testuser", 
            Timestamp = DateTime.UtcNow 
        });
    }
    await context.SaveChangesAsync();
    
    var controller = new MessagesController(
        new MessageService(context),
        new NullLogger<MessagesController>());
    
    // Act
    var result = await controller.GetMessages(page: 1, pageSize: 10);
    
    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    var pagedResult = Assert.IsType<PagedResultDto<MessageDto>>(okResult.Value);
    
    Assert.Equal(10, pagedResult.Items.Count);
    Assert.Equal(25, pagedResult.TotalCount);
    Assert.Equal(3, pagedResult.PageCount);
}
```

## Test Automation

### Continuous Integration

Tests are automatically run in the CI pipeline on every PR and push to main branches:

1. **PR Validation**:
   - Run all unit tests
   - Run linters
   - Generate code coverage report

2. **Main Branch Deployment**:
   - Run all unit and integration tests
   - Run end-to-end tests
   - Generate comprehensive test reports

### Code Coverage

We use the following tools for code coverage:
- Frontend: Jest coverage reports
- Backend: Coverlet with xUnit

Coverage reports are generated and archived in the CI pipeline.

## Performance Testing

Performance testing ensures the application can handle the expected load:

1. **Load Testing**: Simulate multiple users accessing the application
2. **Stress Testing**: Test the application under extreme conditions
3. **Endurance Testing**: Test the application over an extended period

Key performance metrics:
- Response time
- Throughput
- Resource utilization
- Concurrent user capacity

## Security Testing

Security testing identifies vulnerabilities and ensures data protection:

1. **Static Code Analysis**: Analyze code for security issues
2. **Dependency Scanning**: Check for vulnerabilities in dependencies
3. **Penetration Testing**: Attempt to exploit vulnerabilities

Focus areas:
- Input validation
- Authentication and authorization
- Data protection
- XSS prevention
- CSRF protection
- Rate limiting effectiveness

## Manual Testing

While automated tests cover most scenarios, manual testing is still important for:

1. **Exploratory Testing**: Discover unexpected issues
2. **Usability Testing**: Evaluate user experience
3. **Accessibility Testing**: Ensure the application is accessible to all users
4. **Cross-browser Testing**: Verify compatibility across browsers

## Test Data Management

Test data management ensures consistent and reliable test results:

1. **Test Data Generation**: Scripts for generating realistic test data
2. **Database Seeding**: Consistent starting state for tests
3. **Mock Services**: Simulate external dependencies

## Bug Tracking and Resolution

Bug tracking and resolution process:

1. **Bug Reporting**: Document steps to reproduce, expected vs. actual results
2. **Prioritization**: Categorize bugs by severity and impact
3. **Assignment**: Assign bugs to appropriate team members
4. **Verification**: Create tests to verify bug fixes
5. **Regression Testing**: Ensure fixes don't create new issues

## Test Environment Management

Test environment management ensures stable and reproducible testing:

1. **Development**: Local development environment
2. **Testing**: Dedicated testing environment
3. **Staging**: Production-like environment for final testing
4. **Production**: Live environment

## Regression Testing Strategy

Regression testing prevents the reintroduction of previously fixed bugs:

1. **Automated Regression Suite**: Core tests that run on every build
2. **Regression Test Selection**: Smart selection of tests based on code changes
3. **Periodic Full Regression**: Complete testing of all features
