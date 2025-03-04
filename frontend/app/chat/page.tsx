'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import axios from 'axios';
import { io, Socket } from 'socket.io-client';
import MessageItem from '../components/MessageItem';
import ChatInput from '../components/ChatInput';

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
  const [username, setUsername] = useState('');
  const socketRef = useRef<Socket>();
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
        const response = await axios.get('/api/messages');
        const fetchedMessages = response.data.map((msg: any) => ({
          ...msg,
          isCurrentUser: msg.username === storedUsername
        }));
        setMessages(fetchedMessages);
      } catch (error) {
        console.error('Error fetching messages:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchMessages();

    // Set up socket connection
    const socket = io('http://localhost:5000');
    socketRef.current = socket;

    socket.on('connect', () => {
      console.log('Connected to server');
      socket.emit('joinRoom', { username: storedUsername });
    });

    socket.on('message', (message: any) => {
      setMessages(prevMessages => [...prevMessages, {
        ...message,
        isCurrentUser: message.username === storedUsername
      }]);
    });

    return () => {
      socket.disconnect();
    };
  }, [router]);

  // Scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const sendMessage = async (text: string) => {
    if (!text.trim() || !socketRef.current) return;

    try {
      // Send via API to persist
      await axios.post('/api/messages', {
        text,
        username
      });

      // Emit via socket for real-time
      socketRef.current.emit('sendMessage', {
        text,
        username
      });
    } catch (error) {
      console.error('Error sending message:', error);
    }
  };

  return (
    <div className="chat-container">
      <div className="chat-header">
        <h2>Chat Room</h2>
        <div>
          <span>Logged in as: </span>
          <strong>{username}</strong>
        </div>
      </div>
      
      <div className="chat-messages">
        {loading ? (
          <div style={{ textAlign: 'center', padding: '2rem' }}>Loading messages...</div>
        ) : messages.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2rem' }}>No messages yet. Start the conversation!</div>
        ) : (
          messages.map((message) => (
            <MessageItem key={message.id} message={message} />
          ))
        )}
        <div ref={messagesEndRef} />
      </div>
      
      <ChatInput onSendMessage={sendMessage} />
    </div>
  );
}
