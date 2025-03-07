import React, { useState, useRef, useEffect } from 'react';

interface ChatInputProps {
  onSendMessage: (text: string) => void;
  onTypingStatusChange?: (isTyping: boolean) => void;
  disabled?: boolean;
  connectionStatus?: 'connected' | 'connecting' | 'disconnected';
}

const ChatInput: React.FC<ChatInputProps> = ({ 
  onSendMessage, 
  onTypingStatusChange,
  disabled = false,
  connectionStatus = 'connected'
}) => {
  const [message, setMessage] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Focus input when component mounts or when connection is restored
  useEffect(() => {
    if (inputRef.current && !disabled) {
      inputRef.current.focus();
    }
  }, [disabled]);

  const handleTyping = () => {
    // Don't trigger typing events when disconnected
    if (disabled) return;
    
    const wasTyping = isTyping;
    if (!wasTyping) {
      setIsTyping(true);
      
      // Notify parent component about typing status change
      if (onTypingStatusChange) {
        onTypingStatusChange(true);
      }
    }
    
    // Clear existing timeout
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }
    
    // Set a new timeout to stop typing indicator after 2 seconds
    typingTimeoutRef.current = setTimeout(() => {
      setIsTyping(false);
      
      // Notify parent component about typing status change
      if (onTypingStatusChange) {
        onTypingStatusChange(false);
      }
    }, 2000);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (message.trim() && !disabled) {
      onSendMessage(message);
      setMessage('');
      
      // Clear typing timeout and set typing status to false
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
      
      setIsTyping(false);
      
      // Notify parent component about typing status change
      if (onTypingStatusChange) {
        onTypingStatusChange(false);
      }
    }
  };

  // Clean up timeout on unmount
  useEffect(() => {
    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
        
        // Make sure to notify parent that typing stopped when component unmounts
        if (isTyping && onTypingStatusChange) {
          onTypingStatusChange(false);
        }
      }
    };
  }, [isTyping, onTypingStatusChange]);

  // Get placeholder text based on connection status
  const getPlaceholder = () => {
    switch (connectionStatus) {
      case 'connecting':
        return 'Connecting...';
      case 'disconnected':
        return 'Disconnected. Reconnecting...';
      default:
        return 'Type a message...';
    }
  };

  // Get CSS class based on connection status
  const getInputClass = () => {
    let baseClass = 'message-input';
    
    if (disabled) {
      if (connectionStatus === 'connecting') {
        return `${baseClass} connecting`;
      }
      return `${baseClass} disconnected`;
    }
    
    return baseClass;
  };

  return (
    <form className="chat-input" onSubmit={handleSubmit}>
      <div className="input-wrapper">
        <input
          ref={inputRef}
          type="text"
          value={message}
          onChange={(e) => {
            setMessage(e.target.value);
            if (e.target.value.trim()) {
              handleTyping();
            } else if (isTyping) {
              // If field becomes empty, stop typing indication
              setIsTyping(false);
              if (onTypingStatusChange) {
                onTypingStatusChange(false);
              }
              if (typingTimeoutRef.current) {
                clearTimeout(typingTimeoutRef.current);
              }
            }
          }}
          className={getInputClass()}
          placeholder={getPlaceholder()}
          aria-label="Message input"
          disabled={disabled}
          maxLength={1000} // Add reasonable character limit
        />
        {connectionStatus !== 'connected' && (
          <div className="connection-indicator">
            {connectionStatus === 'connecting' ? 'Connecting...' : 'Reconnecting...'}
          </div>
        )}
      </div>
      <button 
        type="submit" 
        disabled={!message.trim() || disabled}
        className={disabled ? 'send-button disabled' : 'send-button'}
        aria-label="Send message"
      >
        Send
      </button>
    </form>
  );
};

export default ChatInput;