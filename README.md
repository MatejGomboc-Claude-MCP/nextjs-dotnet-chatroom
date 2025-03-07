# NextJS + .NET Core Chatroom Application

A simple chatroom web application with a NextJS frontend and .NET Core backend, using MariaDB as the database.

## Tech Stack

- **Frontend**: NextJS 14, SASS, npm
- **Backend**: .NET 8 (ASP.NET Core)
- **Database**: MariaDB 11.2
- **Deployment**: Debian 12 (Bookworm)
- **Testing**: Jest (frontend), xUnit (backend)

## Features

- Real-time chat functionality using SignalR
- Persistent message storage in MariaDB
- Responsive design using SASS
- User authentication (simple username-based)
- Typing indicators with real-time updates
- Join/leave notifications
- Error handling and loading states
- HTTPS support for production
- Pagination for message history
- Rate limiting to prevent abuse
- Health checks for monitoring
- API service layer with proper error handling
- SignalR service for real-time communication
- Content sanitization to prevent XSS attacks
- Error boundaries for graceful error handling
- Security headers for browser protection
- Input validation on both client and server sides
- Automated testing for both frontend and backend
- Reconnection handling for network interruptions
- Auto-scroll with smart behavior based on user interaction
- Active users list with online status
- Message editing and deletion capabilities
- Message reactions with emoji support
- Message search functionality
- Browser notifications for new messages
- Username uniqueness validation
- Read receipts and edit timestamps

## Project Structure

```
./
├── frontend/                  # NextJS frontend application
│   ├── app/                   # Next.js app directory
│   │   ├── components/        # Reusable UI components
│   │   ├── chat/              # Chat page and functionality
│   │   ├── services/          # API and SignalR services
│   │   ├── styles/            # SASS stylesheets
│   ├── public/                # Static assets
│   ├── jest.setup.js          # Jest testing setup
├── backend/                   # .NET Core backend API
│   ├── Controllers/           # API controllers
│   ├── Data/                  # Database context
│   ├── Hubs/                  # SignalR hubs
│   ├── Middleware/            # Custom middleware
│   ├── Models/                # Domain models and DTOs
│   ├── Services/              # Business logic services
│   ├── ChatRoom.Api.Tests/    # Backend unit tests
├── docs/                      # Documentation
└── scripts/                   # Deployment and utility scripts
```

## Development Setup

### Prerequisites

- Node.js 20.x or later
- .NET SDK 8.0 or later
- MariaDB 11.2 or later

### Frontend Setup

```bash
# Navigate to the frontend directory
cd frontend

# Create .env.local file from example
cp .env.local.example .env.local

# Install dependencies
npm install

# Start the development server
npm run dev
```

The frontend will be available at http://localhost:3000.

### Backend Setup

```bash
# Navigate to the backend directory
cd backend

# Restore dependencies
dotnet restore

# Start the development server
dotnet run
```

The backend API will be available at http://localhost:5000.

### Database Setup

1. Install MariaDB on your system
2. Create a new database and user:

```sql
CREATE DATABASE chatroom_dev;
CREATE USER 'chatroomuser'@'localhost' IDENTIFIED BY 'dev_password';
GRANT ALL PRIVILEGES ON chatroom_dev.* TO 'chatroomuser'@'localhost';
FLUSH PRIVILEGES;
```

3. The connection string is already set in `appsettings.Development.json`

## Running Tests

### Frontend Tests

```bash
# Navigate to the frontend directory
cd frontend

# Run tests once
npm test

# Run tests in watch mode during development
npm run test:watch
```

### Backend Tests

```bash
# Navigate to the backend directory
cd backend

# Run tests
dotnet test
```

## Production Deployment on Debian

### Environment Variables

Before deployment, set the following environment variables:

```bash
export REPO_PATH=/path/to/your/repo
export DOMAIN=your-domain.com
export MARIADB_PASSWORD=your_secure_password
export DB_USER=chatroomuser  # Optional, defaults to chatroomuser
export DB_NAME=chatroom      # Optional, defaults to chatroom
```

### Deployment Steps

```bash
# Make the scripts executable
chmod +x scripts/deploy.sh
chmod +x scripts/setup_https.sh
chmod +x scripts/backup_db.sh

# Run the deployment script (as root or with sudo)
sudo ./scripts/deploy.sh

# Set up HTTPS (optional but recommended for production)
sudo ./scripts/setup_https.sh
```

### Database Backups

To set up automatic database backups, add the backup script to cron:

```bash
# Run backups daily at 2 AM
echo "0 2 * * * root MARIADB_PASSWORD=your_secure_password /path/to/repo/scripts/backup_db.sh" > /etc/cron.d/chatroom-backups
```

## API Endpoints

- `GET /api/messages` - Get all messages with pagination
  - Query parameters: `page` (default: 1), `pageSize` (default: 50, max: 100)
- `GET /api/messages/{id}` - Get a specific message by ID
- `POST /api/messages` - Create a new message
- `GET /health` - Health check endpoint

## Real-time Communication

The application uses SignalR for real-time communication. The SignalR hub is available at `/chatHub` with the following methods:

### Client to Server Methods
- `JoinRoom` - Join the chat room
- `SendMessage` - Send a message
- `SendTypingStatus` - Send typing status
- `EditMessage` - Edit an existing message
- `DeleteMessage` - Delete a message
- `AddReaction` - Add an emoji reaction to a message
- `RemoveReaction` - Remove an emoji reaction from a message
- `GetReactions` - Get all reactions for a message
- `GetActiveUsers` - Get a list of active users in the chat

### Server to Client Events
- `message` - Receive a new message
- `typingStatus` - Receive typing status updates
- `messageEdited` - Message edited notification
- `messageDeleted` - Message deleted notification
- `messageReactions` - Updated reactions for a message
- `activeUsers` - Updated list of active users

## Security Features

- Environment-specific configuration
- HTTPS support with automatic certificate renewal
- Security headers for protection against common web vulnerabilities
- Proper error handling with detailed client feedback
- Input validation with regex patterns
- Rate limiting to prevent abuse
- Database connection string stored as environment variable
- Content sanitization to prevent XSS attacks
- CORS configuration with proper origins
- Automatic reconnection with exponential backoff
- Username uniqueness validation
- Proper input sanitization

## Recent Updates

- Added active users list with online status display
- Implemented message editing and deletion capabilities
- Added emoji reactions to messages
- Added message search functionality
- Implemented browser notifications for new messages
- Added typing indicator timeout to prevent ghost indicators
- Fixed username uniqueness validation
- Enhanced error handling in both frontend and backend
- Improved input sanitization to prevent XSS attacks
- Enhanced SignalR service with comprehensive event handling
- Improved reconnection logic with exponential backoff
- Added read receipts and edit timestamps
- Improved responsive design for mobile devices

## Future Improvements

Some potential future enhancements include:

1. Advanced user authentication with user accounts and JWT tokens
2. File sharing capabilities for images and documents
3. Multiple chat rooms support
4. User profiles with avatars and custom settings
5. Message threading for replies to specific messages
6. Rich text formatting support (Markdown)
7. End-to-end encryption for private conversations

## License

MIT