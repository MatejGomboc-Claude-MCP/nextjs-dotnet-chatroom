# Development Guide

This guide provides detailed information for developers working on the chatroom application.

## Development Environment Setup

### Prerequisites

- Node.js 20.x or later
- .NET SDK 8.0 or later
- MariaDB 11.2 or later
- Git
- IDE of choice (Visual Studio, VS Code, etc.)

### Initial Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/nextjs-dotnet-chatroom.git
   cd nextjs-dotnet-chatroom
   ```

2. Set up the frontend:
   ```bash
   cd frontend
   cp .env.local.example .env.local
   # Edit .env.local if needed to match your environment
   npm install
   ```

3. Set up the database:
   ```sql
   CREATE DATABASE chatroom_dev;
   CREATE USER 'chatroomuser'@'localhost' IDENTIFIED BY 'dev_password';
   GRANT ALL PRIVILEGES ON chatroom_dev.* TO 'chatroomuser'@'localhost';
   FLUSH PRIVILEGES;
   ```

4. Configure backend connection string:
   - Open `backend/appsettings.Development.json`
   - Verify that the connection string matches your database settings

### Running the Application

1. Start the backend:
   ```bash
   cd backend
   dotnet run
   ```

2. Start the frontend in a separate terminal:
   ```bash
   cd frontend
   npm run dev
   ```

3. Access the application:
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:5000/api
   - Swagger API docs: http://localhost:5000/swagger

## Development Workflow

### Branching Strategy

- `main`: Production-ready code
- `develop`: Integration branch for features
- `feature/*`: Individual feature branches
- `bugfix/*`: Bug fix branches
- `release/*`: Release preparation branches

### Coding Style Guidelines

#### Frontend (TypeScript/React)

- Use functional components with hooks
- Keep components focused on a single responsibility
- Use TypeScript types for all props and state
- Follow file naming conventions:
  - Components: PascalCase.tsx
  - Utilities: camelCase.ts
  - Styles: componentName.scss

#### Backend (.NET Core)

- Follow C# coding conventions
- Use async/await for asynchronous operations
- Place each class in its own file
- Use dependency injection for services
- Use LINQ for data queries
- Add XML documentation comments to public APIs

### Commit Message Format

```
type: Subject

Body (optional)

Footer (optional)
```

Where type is one of:
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Build process or tooling changes

## Testing

### Frontend Tests

The frontend uses Jest and Testing Library for unit and component tests:

```bash
cd frontend
npm test             # Run all tests
npm run test:watch   # Run tests in watch mode
```

#### Writing Frontend Tests

- Place test files next to the component they test
- Naming convention: `ComponentName.test.tsx`
- Test component rendering, user interactions, and error states
- Mock external dependencies like API calls

Example:
```typescript
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import MessageItem from './MessageItem';

test('renders message correctly', () => {
  render(<MessageItem message={{...}} />);
  expect(screen.getByText('Hello world')).toBeInTheDocument();
});
```

### Backend Tests

The backend uses xUnit for unit and integration tests:

```bash
cd backend
dotnet test
```

#### Writing Backend Tests

- Place tests in the `ChatRoom.Api.Tests` project
- Name test classes after the class they test (e.g., `MessageServiceTests`)
- Use in-memory database for data access tests
- Mock external dependencies

Example:
```csharp
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
}
```

## Debugging

### Frontend Debugging

- Use browser developer tools (F12)
- React Developer Tools extension is recommended
- Console logging for quick debugging
- Use browser breakpoints for step-by-step debugging

### Backend Debugging

- Use the debugger in Visual Studio or VS Code
- Add logging statements with different severity levels
- Check logs in the console output
- Use SQL Server Management Studio or MySQL Workbench to inspect database

## Performance Considerations

- Minimize re-renders in React components
- Use pagination for large data sets
- Implement caching where appropriate
- Keep SignalR payload sizes small
- Batch database operations when possible

## Security Best Practices

- Always sanitize user input
- Use parameterized queries for database access
- Keep dependencies updated
- Implement proper rate limiting
- Use HTTPS in production
- Never commit sensitive information (passwords, API keys)

## Common Issues and Solutions

### "Connection refused" when starting the application

- Ensure MariaDB is running
- Check the connection string in `appsettings.Development.json`
- Verify the database user has proper permissions

### SignalR connection issues

- Check browser console for errors
- Ensure backend is running and accessible
- Verify CORS settings in `Program.cs`
- Check for network restrictions blocking WebSocket connections

### Database migration errors

- Ensure the database exists and is accessible
- Check if the connection string is correct
- Verify the database schema matches the entity models
