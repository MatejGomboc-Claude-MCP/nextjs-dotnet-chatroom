# SignalR Documentation

This document describes the real-time communication aspects of the chatroom application using SignalR.

## Overview

SignalR is used for real-time bidirectional communication between clients and the server. In the chatroom application, SignalR enables:

- Real-time message delivery
- Typing indicators
- Presence notifications (join/leave events)

## Hub URL

- Development: `http://localhost:5000/chatHub`
- Production: `https://your-domain.com/chatHub`

## Client Configuration

### Connection Setup

The frontend establishes a SignalR connection using the following configuration:

```typescript
// Build the connection with resilient options
this.connection = new HubConnectionBuilder()
  .withUrl(HUB_URL, {
    skipNegotiation: false,
    transport: HttpTransportType.WebSockets
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 15000, 30000])
  .configureLogging(LogLevel.Information)
  .build();
```

### Connection Management

The client implements the following connection management features:

- Automatic reconnection with exponential backoff
- Connection state tracking
- Graceful error handling
- Rejoin room functionality after reconnection

## Server Methods (Client → Server)

These methods can be called from the client to send data to the server:

### JoinRoom

Notifies the server that a user has joined the chat room.

- **Parameters**:
  ```json
  {
    "username": "johndoe"
  }
  ```
- **Description**: Called when a user first connects to the chat room. The server broadcasts a system message to all other users.

### SendMessage

Sends a chat message to all connected clients.

- **Parameters**:
  ```json
  {
    "text": "Hello world!",
    "username": "johndoe"
  }
  ```
- **Validation**:
  - `text`: Required, non-empty
  - `username`: Required, must match the connected user
- **Description**: The server validates the message, stores it in the database, and broadcasts it to all connected clients.

### SendTypingStatus

Notifies other users when someone is typing.

- **Parameters**:
  ```json
  {
    "username": "johndoe",
    "isTyping": true
  }
  ```
- **Description**: Broadcasts typing status to all clients except the sender. When `isTyping` is true, it indicates the user started typing. When false, it indicates the user stopped typing.

## Client Methods (Server → Client)

These methods are invoked by the server to send data to clients:

### message

Receives a new chat message.

- **Data**:
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "text": "Hello world!",
    "username": "johndoe",
    "timestamp": "2023-01-01T12:00:00Z"
  }
  ```
- **Description**: Handles incoming messages, including user messages and system notifications (join/leave events).

### typingStatus

Receives typing status updates from other users.

- **Data**:
  ```json
  {
    "username": "johndoe",
    "isTyping": true
  }
  ```
- **Description**: Updates the UI to show which users are currently typing.

## Connection Events

### onreconnecting

Triggered when the connection is lost and SignalR is attempting to reconnect.

- **Description**: Updates the UI to show a reconnecting state and pauses message sending.

### onreconnected

Triggered when the connection is successfully reestablished.

- **Description**: Updates the UI to show connected state, rejoins the chat room, and resumes normal operation.

### onclose

Triggered when the connection is closed.

- **Description**: Updates the UI to show disconnected state and initiates manual reconnection attempts if automatic reconnection fails.

## Error Handling

The SignalR connection implements robust error handling:

- Connection errors trigger automatic reconnection attempts
- Reconnection uses exponential backoff to avoid overwhelming the server
- Errors are logged and displayed to the user when appropriate
- Messages sent during disconnection are handled gracefully

## User Tracking

The server maintains a dictionary of connected users:

```csharp
private static readonly ConcurrentDictionary<string, string> _connectedUsers = new();
```

This enables:
- Tracking who is currently connected
- Sending proper leave notifications when connections are closed
- Maintaining a consistent user list even with connection issues

## Performance Considerations

- Messages are sent to all clients simultaneously
- Typing indicators are only sent to other users, not back to the sender
- Connection issues are handled with graceful degradation
- The server performs input validation to prevent malicious content

## Security Considerations

- Input validation is performed on both client and server sides
- Messages are sanitized to prevent XSS attacks
- Rate limiting is applied to prevent abuse
- Proper error handling avoids leaking sensitive information
