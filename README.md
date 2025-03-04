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

- `JoinRoom` - Join the chat room (client to server)
- `SendMessage` - Send a message (client to server)
- `SendTypingStatus` - Send typing status (client to server)
- `message` - Receive a message (server to client)
- `typingStatus` - Receive typing status (server to client)

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

## Recent Updates

- Fixed SignalR integration on the frontend (replaced Socket.IO)
- Added proper error handling and user feedback
- Improved username validation and input sanitization
- Added typing indicators integration
- Added user leave notifications
- Fixed API URL handling for health checks and other endpoints
- Added error boundaries for React error handling
- Added security headers in Next.js configuration
- Improved environment variable handling for production builds
- Added unit testing for both frontend and backend
- Implemented proper pagination with auto-scroll functionality
- Enhanced SignalR reconnection handling

## License

MIT