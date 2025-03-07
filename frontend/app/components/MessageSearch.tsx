import React, { useState } from 'react';

interface MessageSearchProps {
  onSearch: (query: string) => void;
  onClearSearch: () => void;
  disabled?: boolean;
}

const MessageSearch: React.FC<MessageSearchProps> = ({ 
  onSearch, 
  onClearSearch,
  disabled = false 
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [isExpanded, setIsExpanded] = useState(false);
  
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (searchQuery.trim() && !disabled) {
      onSearch(searchQuery.trim());
    }
  };
  
  const handleClear = () => {
    setSearchQuery('');
    onClearSearch();
  };
  
  const toggleExpand = () => {
    // If disabled, don't allow expanding
    if (disabled && !isExpanded) {
      return;
    }
    
    setIsExpanded(!isExpanded);
    if (isExpanded && searchQuery) {
      handleClear();
    }
  };
  
  return (
    <div className={`message-search ${isExpanded ? 'expanded' : ''} ${disabled ? 'disabled' : ''}`}>
      <button 
        className="search-toggle"
        onClick={toggleExpand}
        aria-label={isExpanded ? "Close search" : "Open search"}
        disabled={disabled && !isExpanded} // Allow closing even when disabled
      >
        <span className="search-icon">🔍</span>
      </button>
      
      {isExpanded && (
        <form onSubmit={handleSubmit} className="search-form">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={disabled ? "Search unavailable" : "Search messages..."}
            aria-label="Search messages"
            className="search-input"
            autoFocus
            disabled={disabled}
          />
          
          {searchQuery && (
            <button 
              type="button" 
              className="clear-search"
              onClick={handleClear}
              aria-label="Clear search"
              disabled={disabled}
            >
              ×
            </button>
          )}
          
          <button 
            type="submit"
            className="submit-search"
            disabled={!searchQuery.trim() || disabled}
            aria-label="Submit search"
          >
            Search
          </button>
        </form>
      )}
    </div>
  );
};

export default MessageSearch;