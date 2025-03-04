import React, { useState, useRef, useEffect } from 'react';

interface ChatInputProps {
  onSendMessage: (text: string) => void;
}

const ChatInput: React.FC<ChatInputProps> = ({ onSendMessage }) => {
  const [message, setMessage] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Focus input when component mounts
  useEffect(() => {
    if (inputRef.current) {
      inputRef.current.focus();
    }
  }, []);

  const handleTyping = () => {
    if (!isTyping) {
      setIsTyping(true);
      // Add code here to emit typing event via SignalR if needed
    }
    
    // Clear existing timeout
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }
    
    // Set a new timeout to stop typing indicator after 2 seconds
    typingTimeoutRef.current = setTimeout(() => {
      setIsTyping(false);
      // Add code here to emit stopped typing event via SignalR if needed
    }, 2000);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (message.trim()) {
      onSendMessage(message);
      setMessage('');
      
      // Clear typing timeout
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
        setIsTyping(false);
      }
    }
  };

  // Clean up timeout on unmount
  useEffect(() => {
    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    };
  }, []);

  return (
    <form className="chat-input" onSubmit={handleSubmit}>
      <input
        ref={inputRef}
        type="text"
        value={message}
        onChange={(e) => {
          setMessage(e.target.value);
          handleTyping();
        }}
        placeholder="Type a message..."
        aria-label="Message input"
      />
      <button 
        type="submit" 
        disabled={!message.trim()}
        aria-label="Send message"
      >
        Send
      </button>
    </form>
  );
};

export default ChatInput;
