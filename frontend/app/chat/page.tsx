'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { 
  chatConnection, 
  Message as SignalRMessage, 
  TypingStatus 
} from '../services/signalr';
import { messagesApi, userApi, handleApiError } from '../services/api';
import MessageItem from '../components/MessageItem';
import ChatInput from '../components/ChatInput';
import TypingIndicator from '../components/TypingIndicator';
import ErrorMessage from '../components/ErrorMessage';
import LoadingSpinner from '../components/LoadingSpinner';
import Pagination from '../components/Pagination';
import UsersList from '../components/UsersList';
import MessageSearch from '../components/MessageSearch';
import { notificationService } from '../services/notifications';
import { ReactionsMap } from '../components/MessageReactions';

interface Message {
  id: string;
  text: string;
  username: string;
  timestamp: string;
  isCurrentUser: boolean;
  isEdited?: boolean;
  editedAt?: string;
}

export default function ChatPage() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [username, setUsername] = useState('');
  const [typingUsers, setTypingUsers] = useState<string[]>([]);
  const [activeUsers, setActiveUsers] = useState<string[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [pageSize] = useState(50);
  const [searching, setSearching] = useState(false);
  const [searchResults, setSearchResults] = useState<Message[]>([]);
  const [messageReactions, setMessageReactions] = useState<{[messageId: string]: ReactionsMap}>({});
  const [showUsersList, setShowUsersList] = useState(false);
  const [notificationsEnabled, setNotificationsEnabled] = useState(false);
  const [windowHasFocus, setWindowHasFocus] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const bottomScrollRef = useRef<HTMLDivElement>(null);
  const [autoScroll, setAutoScroll] = useState(true);
  const router = useRouter();

  // Request notification permissions on component mount
  useEffect(() => {
    const requestPermission = async () => {
      if (notificationService.isSupported()) {
        const hasPermission = await notificationService.requestPermission();
        setNotificationsEnabled(hasPermission);
      }
    };
    
    requestPermission();
  }, []);

  // Track window focus for notifications
  useEffect(() => {
    const handleFocus = () => setWindowHasFocus(true);
    const handleBlur = () => setWindowHasFocus(false);
    
    window.addEventListener('focus', handleFocus);
    window.addEventListener('blur', handleBlur);
    
    return () => {
      window.removeEventListener('focus', handleFocus);
      window.removeEventListener('blur', handleBlur);
    };
  }, []);

  // Fetch messages with pagination
  const fetchMessages = async (page: number = 1) => {
    try {
      setLoadingMore(true);
      const result = await messagesApi.getMessagesPaged(page, pageSize);
      
      const fetchedMessages = result.items.map((msg) => ({
        ...msg,
        isCurrentUser: msg.username === username
      }));
      
      setMessages(fetchedMessages);
      setCurrentPage(result.currentPage);
      setTotalPages(result.pageCount);
      setError(null);
    } catch (err) {
      const apiError = handleApiError(err);
      setError(`Error loading messages: ${apiError.message}`);
      console.error('Error fetching messages:', err);
    } finally {
      setLoadingMore(false);
      setLoading(false);
      
      // Only auto-scroll on the latest page
      if (page === 1 && autoScroll) {
        setTimeout(() => {
          messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
        }, 100);
      }
    }
  };

  // Search messages
  const searchMessages = async (query: string) => {
    try {
      setSearching(true);
      setError(null);
      
      const response = await messagesApi.searchMessages(query);
      
      if (response && response.items) {
        const searchResults = response.items.map((msg) => ({
          ...msg,
          isCurrentUser: msg.username === username
        }));
        
        setSearchResults(searchResults);
      } else {
        setSearchResults([]);
      }
    } catch (err) {
      const apiError = handleApiError(err);
      setError(`Error searching messages: ${apiError.message}`);
      console.error('Error searching messages:', err);
    } finally {
      setSearching(false);
    }
  };

  // Clear search and show all messages
  const clearSearch = () => {
    setSearching(false);
    setSearchResults([]);
  };

  // Initialize SignalR and fetch messages
  useEffect(() => {
    // Check if user has a username set
    const storedUsername = sessionStorage.getItem('chatUsername');
    if (!storedUsername) {
      router.push('/');
      return;
    }

    setUsername(storedUsername);
    
    // Fetch initial messages
    fetchMessages(1);

    // Set up SignalR connection
    const setupSignalR = async () => {
      try {
        // Connection status listener
        const unsubscribeConnection = chatConnection.onConnectionChange((connected) => {
          setIsConnected(connected);
          setConnectionError(connected ? null : 'Connection to chat server lost. Attempting to reconnect...');
        });

        // Message listener
        const unsubscribeMessage = chatConnection.onMessage((message: SignalRMessage) => {
          // Only add new messages if on the first page (most recent)
          if (currentPage === 1 && !searching) {
            setMessages(prevMessages => [...prevMessages, {
              ...message,
              isCurrentUser: message.username === storedUsername
            }]);
            
            // Show notification if window is not focused and message is not from current user
            if (!windowHasFocus && notificationsEnabled && message.username !== storedUsername && message.username !== 'System') {
              notificationService.showNotification(`${message.username}: ${message.text.substring(0, 50)}${message.text.length > 50 ? '...' : ''}`);
            }
          } else if (!searching) {
            // Show notification that new messages are available
            setError('New messages are available. Go to page 1 to see them.');
          }
        });

        // Typing status listener
        const unsubscribeTyping = chatConnection.onTypingStatus((status: TypingStatus) => {
          setTypingUsers(prev => {
            // Remove user if they stopped typing
            if (!status.isTyping) {
              return prev.filter(user => user !== status.username);
            }
            
            // Add user if they started typing and aren't already in the list
            if (!prev.includes(status.username)) {
              return [...prev, status.username];
            }
            
            return prev;
          });
        });

        // Active users listener
        const unsubscribeActiveUsers = chatConnection.onActiveUsers((users: string[]) => {
          setActiveUsers(users);
        });

        // Message edited listener
        const unsubscribeMessageEdited = chatConnection.onMessageEdited((data: { messageId: string, text: string, editedAt: string }) => {
          setMessages(prevMessages => prevMessages.map(msg => 
            msg.id === data.messageId 
              ? { ...msg, text: data.text, isEdited: true, editedAt: data.editedAt } 
              : msg
          ));
          
          setSearchResults(prevResults => prevResults.map(msg => 
            msg.id === data.messageId 
              ? { ...msg, text: data.text, isEdited: true, editedAt: data.editedAt } 
              : msg
          ));
        });

        // Message deleted listener
        const unsubscribeMessageDeleted = chatConnection.onMessageDeleted((data: { messageId: string }) => {
          setMessages(prevMessages => prevMessages.filter(msg => msg.id !== data.messageId));
          setSearchResults(prevResults => prevResults.filter(msg => msg.id !== data.messageId));
        });

        // Message reactions listener
        const unsubscribeMessageReactions = chatConnection.onMessageReactions((data: { messageId: string, reactions: ReactionsMap }) => {
          setMessageReactions(prev => ({
            ...prev,
            [data.messageId]: data.reactions
          }));
        });

        // Start connection
        await chatConnection.start(storedUsername);

        // Return cleanup function
        return () => {
          unsubscribeConnection();
          unsubscribeMessage();
          unsubscribeTyping();
          unsubscribeActiveUsers();
          unsubscribeMessageEdited();
          unsubscribeMessageDeleted();
          unsubscribeMessageReactions();
          chatConnection.stop();
        };
      } catch (err) {
        setConnectionError('Failed to connect to chat server. Please try again later.');
        console.error('SignalR connection error:', err);
        return () => {};
      }
    };

    const cleanup = setupSignalR();
    
    return () => {
      cleanup.then(cleanupFn => cleanupFn());
    };
  }, [router, currentPage, searching]);

  // Handle page change
  const handlePageChange = (page: number) => {
    if (page !== currentPage) {
      fetchMessages(page);
      setAutoScroll(page === 1); // Only auto-scroll on the first (latest) page
    }
  };

  // Scroll to bottom when new messages arrive on page 1
  useEffect(() => {
    if (currentPage === 1 && autoScroll && !searching) {
      messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages, currentPage, autoScroll, searching]);

  // Handle scroll events to determine if auto-scroll should be enabled
  useEffect(() => {
    const handleScroll = () => {
      if (!bottomScrollRef.current) return;
      
      // Check if user has scrolled up
      const { scrollTop, scrollHeight, clientHeight } = bottomScrollRef.current;
      const isScrolledToBottom = scrollHeight - scrollTop - clientHeight < 50;
      
      setAutoScroll(isScrolledToBottom);
    };
    
    const messageContainer = bottomScrollRef.current;
    if (messageContainer) {
      messageContainer.addEventListener('scroll', handleScroll);
      return () => messageContainer.removeEventListener('scroll', handleScroll);
    }
  }, []);

  const sendMessage = async (text: string) => {
    if (!text.trim() || !isConnected) return;

    try {
      // Send message via SignalR
      await chatConnection.sendMessage(text, username);
      
      // If searching, clear search to see the sent message
      if (searching) {
        clearSearch();
      }
      
      // If not on page 1, go to page 1 to see the sent message
      if (currentPage !== 1) {
        handlePageChange(1);
      }
    } catch (err) {
      setError(`Failed to send message: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error sending message:', err);
    }
  };

  const sendTypingStatus = async (isTyping: boolean) => {
    if (!isConnected) return;
    
    try {
      await chatConnection.sendTypingStatus(username, isTyping);
    } catch (err) {
      // Don't show error for typing status failures
      console.error('Error sending typing status:', err);
    }
  };

  const editMessage = async (messageId: string, newText: string) => {
    if (!newText.trim() || !isConnected) return;
    
    try {
      await chatConnection.editMessage(messageId, newText, username);
    } catch (err) {
      setError(`Failed to edit message: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error editing message:', err);
    }
  };

  const deleteMessage = async (messageId: string) => {
    if (!isConnected) return;
    
    try {
      await chatConnection.deleteMessage(messageId, username);
    } catch (err) {
      setError(`Failed to delete message: ${err instanceof Error ? err.message : 'Unknown error'}`);
      console.error('Error deleting message:', err);
    }
  };

  const addReaction = async (messageId: string, emoji: string) => {
    if (!isConnected) return;
    
    try {
      await chatConnection.addReaction(messageId, emoji, username);
    } catch (err) {
      console.error('Error adding reaction:', err);
    }
  };

  const removeReaction = async (messageId: string, emoji: string) => {
    if (!isConnected) return;
    
    try {
      await chatConnection.removeReaction(messageId, emoji, username);
    } catch (err) {
      console.error('Error removing reaction:', err);
    }
  };

  // Toggle users list visibility
  const toggleUsersList = () => {
    setShowUsersList(!showUsersList);
  };

  return (
    <div className="chat-container">
      <div className="chat-header">
        <h2>Chat Room</h2>
        <div className="header-actions">
          <MessageSearch 
            onSearch={searchMessages}
            onClearSearch={clearSearch}
          />
          <button
            className="toggle-users-list"
            onClick={toggleUsersList}
            aria-label={showUsersList ? "Hide users list" : "Show users list"}
          >
            <span className="users-icon">👥</span>
            <span className="users-count">{activeUsers.length}</span>
          </button>
        </div>
        <div className="user-info">
          <span>Logged in as: </span>
          <strong>{username}</strong>
          <span className={`connection-status ${isConnected ? 'connected' : 'disconnected'}`}>
            {isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
      </div>
      
      {connectionError && <ErrorMessage message={connectionError} />}
      {error && <ErrorMessage message={error} />}
      
      <div className="chat-layout">
        <div className="chat-content">
          {totalPages > 1 && !searching && (
            <div className="pagination-container">
              <Pagination
                currentPage={currentPage}
                totalPages={totalPages}
                onPageChange={handlePageChange}
              />
            </div>
          )}
          
          {searching && (
            <div className="search-info">
              <button className="back-button" onClick={clearSearch}>
                ← Back to all messages
              </button>
              <p className="search-results-info">
                {searchResults.length} {searchResults.length === 1 ? 'result' : 'results'} found
              </p>
            </div>
          )}
          
          <div className="chat-messages" ref={bottomScrollRef}>
            {loading ? (
              <LoadingSpinner />
            ) : searching ? (
              searchResults.length === 0 ? (
                <div className="empty-state">No messages found. Try a different search term.</div>
              ) : (
                <>
                  {searchResults.map((message) => (
                    <MessageItem 
                      key={message.id} 
                      message={message}
                      onEditMessage={editMessage}
                      onDeleteMessage={deleteMessage}
                      reactions={messageReactions[message.id] || {}}
                      onAddReaction={addReaction}
                      onRemoveReaction={removeReaction}
                      currentUsername={username}
                    />
                  ))}
                </>
              )
            ) : loadingMore ? (
              <div className="loading-more">
                <LoadingSpinner />
                <p>Loading messages...</p>
              </div>
            ) : messages.length === 0 ? (
              <div className="empty-state">No messages yet. Start the conversation!</div>
            ) : (
              <>
                {messages.map((message) => (
                  <MessageItem 
                    key={message.id} 
                    message={message}
                    onEditMessage={editMessage}
                    onDeleteMessage={deleteMessage}
                    reactions={messageReactions[message.id] || {}}
                    onAddReaction={addReaction}
                    onRemoveReaction={removeReaction}
                    currentUsername={username}
                  />
                ))}
                <div ref={messagesEndRef} />
              </>
            )}
          </div>
          
          {!autoScroll && currentPage === 1 && !searching && (
            <button 
              className="scroll-to-bottom-button"
              onClick={() => {
                messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
                setAutoScroll(true);
              }}
              aria-label="Scroll to bottom"
            >
              New messages ↓
            </button>
          )}
          
          {typingUsers.length > 0 && <TypingIndicator typingUsers={typingUsers.filter(user => user !== username)} />}
          
          <ChatInput 
            onSendMessage={sendMessage} 
            onTypingStatusChange={sendTypingStatus}
            disabled={!isConnected}
          />
        </div>
        
        {showUsersList && (
          <div className="chat-sidebar">
            <UsersList users={activeUsers} currentUser={username} />
          </div>
        )}
      </div>
    </div>
  );
}