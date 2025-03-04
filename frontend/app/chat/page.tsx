'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { chatConnection, Message as SignalRMessage, TypingStatus } from '../services/signalr';
import { messagesApi, ApiError, handleApiError } from '../services/api';
import MessageItem from '../components/MessageItem';
import ChatInput from '../components/ChatInput';
import TypingIndicator from '../components/TypingIndicator';
import ErrorMessage from '../components/ErrorMessage';
import LoadingSpinner from '../components/LoadingSpinner';

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
  const [error, setError] = useState<string | null>(null);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [username, setUsername] = useState('');
  const [typingUsers, setTypingUsers] = useState<string[]>([]);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const router = useRouter();

  useEffect(() => {
    // Check if user has a username set
    const storedUsername = sessionStorage.getItem('chatUsername');
    if (!storedUsername) {
      router.push('/');
      return;
    }

    setUsername(storedUsername);
    
    // Fetch previous messages
    const fetchMessages = async () => {
      try {
        setLoading(true);
        const result = await messagesApi.getMessagesPaged();
        const fetchedMessages = result.items.map((msg) => ({
          ...msg,
          isCurrentUser: msg.username === storedUsername
        }));
        setMessages(fetchedMessages);
        setError(null);
      } catch (err) {
        const apiError = handleApiError(err);
        setError(`Error loading messages: ${apiError.message}`);
        console.error('Error fetching messages:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchMessages();

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
          setMessages(prevMessages => [...prevMessages, {
            ...message,
            isCurrentUser: message.username === storedUsername
          }]);
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

  // Scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const sendMessage = async (text: string) => {
    if (!text.trim() || !isConnected) return;

    try {
      // Send message via SignalR
      await chatConnection.sendMessage(text, username);
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
      
      <div className="chat-messages">
        {loading ? (
          <LoadingSpinner />
        ) : messages.length === 0 ? (
          <div className="empty-state">No messages yet. Start the conversation!</div>
        ) : (
          messages.map((message) => (
            <MessageItem key={message.id} message={message} />
          ))
        )}
        <div ref={messagesEndRef} />
      </div>
      
      {typingUsers.length > 0 && <TypingIndicator typingUsers={typingUsers.filter(user => user !== username)} />}
      
      <ChatInput 
        onSendMessage={sendMessage} 
        onTypingStatusChange={sendTypingStatus}
        disabled={!isConnected}
      />
    </div>
  );
}