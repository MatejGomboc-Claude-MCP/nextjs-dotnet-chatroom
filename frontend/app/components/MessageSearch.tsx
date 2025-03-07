import React, { useState } from 'react';

interface MessageSearchProps {
  onSearch: (query: string) => void;
  onClearSearch: () => void;
}

const MessageSearch: React.FC<MessageSearchProps> = ({ onSearch, onClearSearch }) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [isExpanded, setIsExpanded] = useState(false);
  
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (searchQuery.trim()) {
      onSearch(searchQuery.trim());
    }
  };
  
  const handleClear = () => {
    setSearchQuery('');
    onClearSearch();
  };
  
  const toggleExpand = () => {
    setIsExpanded(!isExpanded);
    if (isExpanded && searchQuery) {
      handleClear();
    }
  };
  
  return (
    <div className={`message-search ${isExpanded ? 'expanded' : ''}`}>
      <button 
        className="search-toggle"
        onClick={toggleExpand}
        aria-label={isExpanded ? "Close search" : "Open search"}
      >
        <span className="search-icon">🔍</span>
      </button>
      
      {isExpanded && (
        <form onSubmit={handleSubmit} className="search-form">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search messages..."
            aria-label="Search messages"
            className="search-input"
            autoFocus
          />
          
          {searchQuery && (
            <button 
              type="button" 
              className="clear-search"
              onClick={handleClear}
              aria-label="Clear search"
            >
              ×
            </button>
          )}
          
          <button 
            type="submit"
            className="submit-search"
            disabled={!searchQuery.trim()}
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