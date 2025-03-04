# API Documentation

This document provides details about the REST API endpoints available in the chatroom application.

## Base URL

All API endpoints are relative to the base URL:

- Development: `http://localhost:5000/api`
- Production: `https://your-domain.com/api`

## Authentication

Currently, the API uses simple username-based authentication with no tokens required. For a production application, proper authentication should be implemented.

## Endpoints

### Messages

#### Get Messages (Paginated)

Retrieves a paginated list of chat messages.

- **URL**: `/messages`
- **Method**: `GET`
- **URL Parameters**:
  - `page` (optional): Page number, starting from 1 (default: 1)
  - `pageSize` (optional): Number of messages per page (default: 50, max: 100)
- **Success Response**:
  - **Code**: 200 OK
  - **Content Example**:
    ```json
    {
      "items": [
        {
          "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "text": "Hello world!",
          "username": "johndoe",
          "timestamp": "2023-01-01T12:00:00Z"
        },
        {
          "id": "5a85f64-5717-4562-b3fc-2c963f66afb7",
          "text": "How are you?",
          "username": "janedoe",
          "timestamp": "2023-01-01T12:01:00Z"
        }
      ],
      "totalCount": 125,
      "pageCount": 3,
      "currentPage": 1,
      "pageSize": 50,
      "hasNext": true,
      "hasPrevious": false
    }
    ```
- **Error Responses**:
  - **Code**: 400 Bad Request
    - **Content**: `{ "error": "Invalid page parameter" }`
  - **Code**: 500 Internal Server Error
    - **Content**: `{ "error": "An error occurred while retrieving messages." }`

#### Get Message by ID

Retrieves a specific message by its ID.

- **URL**: `/messages/{id}`
- **Method**: `GET`
- **URL Parameters**:
  - `id`: The UUID of the message
- **Success Response**:
  - **Code**: 200 OK
  - **Content Example**:
    ```json
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "text": "Hello world!",
      "username": "johndoe",
      "timestamp": "2023-01-01T12:00:00Z"
    }
    ```
- **Error Responses**:
  - **Code**: 404 Not Found
    - **Content**: `{ "error": "Message with ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found." }`
  - **Code**: 500 Internal Server Error
    - **Content**: `{ "error": "An error occurred while retrieving the message." }`

#### Create Message

Creates a new chat message.

- **URL**: `/messages`
- **Method**: `POST`
- **Rate Limiting**: 30 requests per minute
- **Request Body**:
  ```json
  {
    "text": "Hello world!",
    "username": "johndoe"
  }
  ```
- **Validation Rules**:
  - `text`: Required, 1-1000 characters
  - `username`: Required, 3-20 characters, alphanumeric and underscores only
- **Success Response**:
  - **Code**: 201 Created
  - **Content Example**:
    ```json
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "text": "Hello world!",
      "username": "johndoe",
      "timestamp": "2023-01-01T12:00:00Z"
    }
    ```
- **Error Responses**:
  - **Code**: 400 Bad Request
    - **Content**: `{ "error": "The Text field is required." }`
  - **Code**: 429 Too Many Requests
    - **Content**: `{ "error": "Too many requests. Please try again later." }`
  - **Code**: 500 Internal Server Error
    - **Content**: `{ "error": "An error occurred while creating the message." }`

### Health

#### Health Check

Checks the overall health of the application and its dependencies.

- **URL**: `/health`
- **Method**: `GET`
- **Success Response**:
  - **Code**: 200 OK
  - **Content Example**:
    ```json
    {
      "status": "Healthy",
      "components": {
        "self": {
          "status": "Healthy"
        },
        "database": {
          "status": "Healthy"
        }
      }
    }
    ```
- **Error Response**:
  - **Code**: 503 Service Unavailable
  - **Content Example**:
    ```json
    {
      "status": "Unhealthy",
      "components": {
        "self": {
          "status": "Healthy"
        },
        "database": {
          "status": "Unhealthy",
          "description": "Unable to connect to database"
        }
      }
    }
    ```

## Error Handling

All API endpoints follow a consistent error handling approach:

1. Validation errors return 400 Bad Request with details about the validation failure
2. Not found errors return 404 Not Found
3. Rate limiting errors return 429 Too Many Requests
4. Server errors return 500 Internal Server Error

Error responses always include an `error` property with a descriptive message.

## Rate Limiting

Rate limiting is implemented to prevent abuse:

- Global rate limit: 100 requests per minute per IP address
- Message creation: 30 requests per minute per IP address

When rate limits are exceeded, the API returns a 429 Too Many Requests response.
