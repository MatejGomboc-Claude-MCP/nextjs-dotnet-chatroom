import React from 'react';

interface TypingIndicatorProps {
  typingUsers: string[];
}

const TypingIndicator: React.FC<TypingIndicatorProps> = ({ typingUsers }) => {
  if (typingUsers.length === 0) {
    return null;
  }

  // Determine what text to show based on how many users are typing
  let typingText = '';
  if (typingUsers.length === 1) {
    typingText = `${typingUsers[0]} is typing...`;
  } else if (typingUsers.length === 2) {
    typingText = `${typingUsers[0]} and ${typingUsers[1]} are typing...`;
  } else if (typingUsers.length === 3) {
    typingText = `${typingUsers[0]}, ${typingUsers[1]}, and ${typingUsers[2]} are typing...`;
  } else {
    typingText = `${typingUsers.length} people are typing...`;
  }

  return (
    <div className="typing-indicator" aria-live="polite">
      <div className="typing-indicator-bubbles">
        <span></span>
        <span></span>
        <span></span>
      </div>
      <div className="typing-indicator-text">
        {typingText}
      </div>
    </div>
  );
};

export default TypingIndicator;
