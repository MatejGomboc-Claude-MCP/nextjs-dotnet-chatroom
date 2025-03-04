# NextJS + .NET Core Chatroom Application

A simple chatroom web application with a NextJS frontend and .NET Core backend, using MariaDB as the database.

## Tech Stack

- **Frontend**: NextJS 14, SASS, npm
- **Backend**: .NET 8 (ASP.NET Core)
- **Database**: MariaDB 11.2
- **Deployment**: Debian 12 (Bookworm)

## Features

- Real-time chat functionality using SignalR
- Persistent message storage in MariaDB
- Responsive design using SASS
- User authentication (simple username-based)
- Typing indicators
- Join/leave notifications

## Project Structure

```
./
├── frontend/      # NextJS frontend application
├── backend/       # .NET Core backend API
├── docs/          # Documentation
└── scripts/       # Deployment scripts
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
CREATE DATABASE chatroom;
CREATE USER 'chatroomuser'@'localhost' IDENTIFIED BY 'your_password';
GRANT ALL PRIVILEGES ON chatroom.* TO 'chatroomuser'@'localhost';
FLUSH PRIVILEGES;
```

3. Update the connection string in `backend/appsettings.json` with your database details

## Production Deployment on Debian

See the `docs/deployment.md` file for detailed deployment instructions. 

Quick deployment:

```bash
# Make the deployment script executable
chmod +x scripts/deploy.sh

# Run the deployment script (as root or with sudo)
sudo ./scripts/deploy.sh
```

## API Endpoints

- `GET /api/messages` - Get all messages
- `GET /api/messages/{id}` - Get a specific message by ID
- `POST /api/messages` - Create a new message

## Real-time Communication

The application uses SignalR for real-time communication. The SignalR hub is available at `/chatHub`.

## License

MIT
