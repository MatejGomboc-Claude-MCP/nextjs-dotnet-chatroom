import React from 'react';

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
  const formattedTime = new Date(message.timestamp).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit'
  });

  return (
    <div className={messageClass}>
      <div className="message-header">
        <span>{message.username}</span>
        <span>{formattedTime}</span>
      </div>
      <div className="message-content">{message.text}</div>
    </div>
  );
};

export default MessageItem;
