import React, { useState } from 'react';

interface MessageActionsProps {
  messageId: string;
  isCurrentUserMessage: boolean;
  onEdit: (messageId: string) => void;
  onDelete: (messageId: string) => void;
}

const MessageActions: React.FC<MessageActionsProps> = ({
  messageId,
  isCurrentUserMessage,
  onEdit,
  onDelete
}) => {
  const [isOpen, setIsOpen] = useState(false);
  
  // Only show actions for current user's messages
  if (!isCurrentUserMessage) {
    return null;
  }
  
  const toggleDropdown = () => {
    setIsOpen(!isOpen);
  };
  
  const handleEdit = () => {
    onEdit(messageId);
    setIsOpen(false);
  };
  
  const handleDelete = () => {
    if (window.confirm('Are you sure you want to delete this message?')) {
      onDelete(messageId);
    }
    setIsOpen(false);
  };
  
  return (
    <div className="message-actions">
      <button 
        className="message-actions-toggle" 
        onClick={toggleDropdown}
        aria-label="Message actions"
        aria-expanded={isOpen}
      >
        <span className="dots">⋮</span>
      </button>
      
      {isOpen && (
        <div className="message-actions-dropdown">
          <button 
            className="message-action-btn edit-btn" 
            onClick={handleEdit}
          >
            Edit
          </button>
          <button 
            className="message-action-btn delete-btn" 
            onClick={handleDelete}
          >
            Delete
          </button>
        </div>
      )}
    </div>
  );
};

export default MessageActions;