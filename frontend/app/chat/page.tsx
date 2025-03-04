'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { chatConnection, Message as SignalRMessage, TypingStatus } from '../services/signalr';
import { messagesApi, ApiError, handleApiError, PagedResult } from '../services/api';
import MessageItem from '../components/MessageItem';
import ChatInput from '../components/ChatInput';
import TypingIndicator from '../components/TypingIndicator';
import ErrorMessage from '../components/ErrorMessage';
import LoadingSpinner from '../components/LoadingSpinner';
import Pagination from '../components/Pagination';

interface Message {
  id: string;
  text: string;
  username: string;
  timestamp: string;
  isCurrentUser: boolean;
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
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [pageSize] = useState(50);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const bottomScrollRef = useRef<HTMLDivElement>(null);
  const [autoScroll, setAutoScroll] = useState(true);
  const router = useRouter();

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
          if (currentPage === 1) {
            setMessages(prevMessages => [...prevMessages, {
              ...message,
              isCurrentUser: message.username === storedUsername
            }]);
          } else {
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

        // Start connection
        await chatConnection.start(storedUsername);

        // Return cleanup function
        return () => {
          unsubscribeConnection();
          unsubscribeMessage();
          unsubscribeTyping();
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
  }, [router]);

  // Handle page change
  const handlePageChange = (page: number) => {
    if (page !== currentPage) {
      fetchMessages(page);
      setAutoScroll(page === 1); // Only auto-scroll on the first (latest) page
    }
  };

  // Scroll to bottom when new messages arrive on page 1
  useEffect(() => {
    if (currentPage === 1 && autoScroll) {
      messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [messages, currentPage, autoScroll]);

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

  return (
    <div className="chat-container">
      <div className="chat-header">
        <h2>Chat Room</h2>
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
      
      {totalPages > 1 && (
        <div className="pagination-container">
          <Pagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={handlePageChange}
          />
        </div>
      )}
      
      <div className="chat-messages" ref={bottomScrollRef}>
        {loading ? (
          <LoadingSpinner />
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
              <MessageItem key={message.id} message={message} />
            ))}
            <div ref={messagesEndRef} />
          </>
        )}
      </div>
      
      {!autoScroll && currentPage === 1 && (
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
  );
}