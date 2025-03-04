# Architecture Overview

This document provides an overview of the chatroom application architecture.

## System Architecture

The chatroom application follows a client-server architecture with three main components:

1. **Frontend**: NextJS React application
2. **Backend**: .NET Core API
3. **Database**: MariaDB

```
+---------------+      +----------------+      +----------------+
|               |      |                |      |                |
|  NextJS       |<---->|  .NET Core     |<---->|  MariaDB       |
|  Frontend     |      |  Backend API   |      |  Database      |
|               |      |                |      |                |
+---------------+      +----------------+      +----------------+
        ^                      ^
        |                      |
   User Interface       Data Persistence
```

## Communication Flow

1. **HTTP Requests**: For initial data loading, authentication, and REST API operations
2. **WebSockets (SignalR)**: For real-time chat functionality including messages, typing indicators, and presence notifications

## Frontend Architecture

The frontend follows a component-based architecture using React with NextJS:

```
+----------------------------------------+
|  NextJS App                            |
|  +----------------------------------+  |
|  |  Components                      |  |
|  |  +----------------------------+  |  |
|  |  |  Pages                     |  |  |
|  |  |  - Home/Login              |  |  |
|  |  |  - Chat                    |  |  |
|  |  +----------------------------+  |  |
|  |                                  |  |
|  |  +----------------------------+  |  |
|  |  |  Shared Components         |  |  |
|  |  |  - MessageItem             |  |  |
|  |  |  - ChatInput               |  |  |
|  |  |  - TypingIndicator         |  |  |
|  |  |  - ErrorBoundary           |  |  |
|  |  +----------------------------+  |  |
|  +----------------------------------+  |
|                                        |
|  +----------------------------------+  |
|  |  Services                        |  |
|  |  - API Service                   |  |
|  |  - SignalR Service               |  |
|  +----------------------------------+  |
+----------------------------------------+
```

## Backend Architecture

The backend follows a layered architecture with clear separation of concerns:

```
+----------------------------------------+
|  API Layer                             |
|  - Controllers                         |
|  - SignalR Hubs                        |
+----------------------------------------+
                  |
+----------------------------------------+
|  Service Layer                         |
|  - Message Service                     |
|  - Other Business Logic                |
+----------------------------------------+
                  |
+----------------------------------------+
|  Data Access Layer                     |
|  - Entity Framework Context            |
|  - Repositories                        |
+----------------------------------------+
                  |
+----------------------------------------+
|  Database                              |
|  - MariaDB                             |
+----------------------------------------+
```

## Data Model

The primary data model includes:

- **Messages**: The core entity representing chat messages
  - Id: Unique identifier
  - Text: Message content
  - Username: Sender's username
  - Timestamp: When the message was sent

## Real-time Communication

SignalR enables real-time communication with the following features:

- **Message Broadcasting**: When a user sends a message, it's broadcasted to all connected clients
- **Typing Indicators**: Notifies other users when someone is typing
- **Presence Notifications**: Alerts when users join or leave the chat

## Security Measures

The application implements various security measures:

- **Input Validation**: On both client and server sides
- **Content Sanitization**: Prevents XSS attacks
- **HTTPS**: For secure communication
- **Rate Limiting**: Prevents abuse
- **Security Headers**: Protection against common web vulnerabilities

## Error Handling

Error handling occurs at multiple levels:

- **Client-side**: React error boundaries and service error handling
- **API**: Exception middleware and standardized error responses
- **SignalR**: Connection resilience with automatic reconnection
