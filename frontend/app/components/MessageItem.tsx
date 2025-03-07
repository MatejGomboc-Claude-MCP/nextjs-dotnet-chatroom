import React, { useState } from 'react';
import DOMPurify from 'dompurify';
import MessageActions from './MessageActions';

interface MessageProps {
  message: {
    id: string;
    text: string;
    username: string;
    timestamp: string;
    isCurrentUser: boolean;
    isEdited?: boolean;
    editedAt?: string;
  };
  onEditMessage?: (messageId: string, newText: string) => Promise<void>;
  onDeleteMessage?: (messageId: string) => Promise<void>;
}

const MessageItem: React.FC<MessageProps> = ({ 
  message, 
  onEditMessage, 
  onDeleteMessage 
}) => {
  const [isEditing, setIsEditing] = useState(false);
  const [editText, setEditText] = useState(message.text);
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const messageClass = message.isCurrentUser ? 'message sent' : 'message received';
  const isSystemMessage = message.username === 'System';
  
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
  
  const handleEdit = () => {
    if (!isSystemMessage && message.isCurrentUser) {
      setEditText(message.text);
      setIsEditing(true);
    }
  };
  
  const handleDelete = async (messageId: string) => {
    if (onDeleteMessage && !isSystemMessage && message.isCurrentUser) {
      try {
        await onDeleteMessage(messageId);
      } catch (error) {
        console.error('Error deleting message:', error);
        alert('Failed to delete message. Please try again.');
      }
    }
  };
  
  const handleSubmitEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!editText.trim() || !onEditMessage) {
      return;
    }
    
    setIsSubmitting(true);
    try {
      await onEditMessage(message.id, editText);
      setIsEditing(false);
    } catch (error) {
      console.error('Error editing message:', error);
      alert('Failed to edit message. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };
  
  const handleCancelEdit = () => {
    setIsEditing(false);
    setEditText(message.text);
  };

  return (
    <div className={messageClass}>
      <div className="message-header">
        <span className="username">{sanitizedUsername}</span>
        <span className="timestamp">
          {formattedTime}
          {message.isEdited && <span className="edited-indicator"> (edited)</span>}
        </span>
        
        {!isSystemMessage && message.isCurrentUser && onEditMessage && onDeleteMessage && (
          <MessageActions 
            messageId={message.id}
            isCurrentUserMessage={message.isCurrentUser}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />
        )}
      </div>
      
      {isEditing ? (
        <form onSubmit={handleSubmitEdit} className="edit-message-form">
          <input
            type="text"
            value={editText}
            onChange={(e) => setEditText(e.target.value)}
            className="edit-message-input"
            autoFocus
            maxLength={1000}
            disabled={isSubmitting}
          />
          <div className="edit-message-actions">
            <button 
              type="button" 
              onClick={handleCancelEdit}
              className="cancel-edit-btn"
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button 
              type="submit" 
              className="save-edit-btn"
              disabled={!editText.trim() || isSubmitting}
            >
              {isSubmitting ? 'Saving...' : 'Save'}
            </button>
          </div>
        </form>
      ) : (
        <div className="message-content">{sanitizedText}</div>
      )}
    </div>
  );
};

export default MessageItem;