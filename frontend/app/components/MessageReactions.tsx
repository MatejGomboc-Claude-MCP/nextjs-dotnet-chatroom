import React, { useState } from 'react';

export interface Reaction {
  emoji: string;
  count: number;
  usernames: string[];
}

export interface ReactionsMap {
  [key: string]: Reaction;
}

interface MessageReactionsProps {
  messageId: string;
  reactions: ReactionsMap;
  currentUsername: string;
  onAddReaction: (messageId: string, emoji: string) => Promise<void>;
  onRemoveReaction: (messageId: string, emoji: string) => Promise<void>;
}

const AVAILABLE_EMOJIS = ['👍', '❤️', '😂', '😮', '😢', '👏'];

const MessageReactions: React.FC<MessageReactionsProps> = ({
  messageId,
  reactions,
  currentUsername,
  onAddReaction,
  onRemoveReaction
}) => {
  const [showSelector, setShowSelector] = useState(false);
  
  const toggleSelector = () => {
    setShowSelector(!showSelector);
  };
  
  const handleAddReaction = async (emoji: string) => {
    try {
      await onAddReaction(messageId, emoji);
      setShowSelector(false);
    } catch (error) {
      console.error('Error adding reaction:', error);
    }
  };
  
  const handleRemoveReaction = async (emoji: string) => {
    try {
      await onRemoveReaction(messageId, emoji);
    } catch (error) {
      console.error('Error removing reaction:', error);
    }
  };
  
  const hasReacted = (emoji: string): boolean => {
    return reactions[emoji]?.usernames.includes(currentUsername) || false;
  };
  
  return (
    <div className="message-reactions">
      <div className="reactions-list">
        {Object.entries(reactions).map(([emoji, reaction]) => (
          reaction.count > 0 && (
            <button
              key={emoji}
              className={`reaction-item ${hasReacted(emoji) ? 'user-reacted' : ''}`}
              onClick={() => hasReacted(emoji) ? handleRemoveReaction(emoji) : handleAddReaction(emoji)}
              aria-label={`${emoji} reaction (${reaction.count})`}
              title={reaction.usernames.join(', ')}
            >
              <span className="reaction-emoji">{emoji}</span>
              <span className="reaction-count">{reaction.count}</span>
            </button>
          )
        ))}
      </div>
      
      <div className="reaction-selector-wrapper">
        <button
          className="add-reaction-btn"
          onClick={toggleSelector}
          aria-label="Add reaction"
          aria-expanded={showSelector}
        >
          <span className="add-reaction-icon">😀</span>
        </button>
        
        {showSelector && (
          <div className="reaction-selector">
            {AVAILABLE_EMOJIS.map(emoji => (
              <button
                key={emoji}
                className={`emoji-option ${hasReacted(emoji) ? 'selected' : ''}`}
                onClick={() => hasReacted(emoji) ? handleRemoveReaction(emoji) : handleAddReaction(emoji)}
                aria-label={`Add ${emoji} reaction`}
              >
                {emoji}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default MessageReactions;