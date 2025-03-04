import React from 'react';
import DOMPurify from 'dompurify';

interface MessageProps {
  message: {
    id: string;
    text: string;
    username: string;
    timestamp: string;
    isCurrentUser: boolean;
  };
}

const MessageItem: React.FC<MessageProps> = ({ message }) => {
  const messageClass = message.isCurrentUser ? 'message sent' : 'message received';
  
  let formattedTime;
  try {
    formattedTime = new Date(message.timestamp).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit'
    });
  } catch (error) {
    formattedTime = 'Invalid time';
    console.error('Error parsing timestamp:', error);
  }

  // Sanitize the message text to prevent XSS attacks
  const sanitizedText = DOMPurify.sanitize(message.text);
  
  // Sanitize the username as well
  const sanitizedUsername = DOMPurify.sanitize(message.username);

  return (
    <div className={messageClass}>
      <div className="message-header">
        <span className="username">{sanitizedUsername}</span>
        <span className="timestamp">{formattedTime}</span>
      </div>
      <div className="message-content">{sanitizedText}</div>
    </div>
  );
};

export default MessageItem;