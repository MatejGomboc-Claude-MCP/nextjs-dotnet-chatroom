'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { healthApi } from './services/api';
import ErrorMessage from './components/ErrorMessage';

export default function Home() {
  const [username, setUsername] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [isServerOnline, setIsServerOnline] = useState(true);
  const router = useRouter();

  // Check if the server is online when component mounts
  useEffect(() => {
    const checkServerStatus = async () => {
      const isOnline = await healthApi.checkHealth();
      setIsServerOnline(isOnline);
      if (!isOnline) {
        setError('Chat server is currently unavailable. Please try again later.');
      }
    };
    
    checkServerStatus();
  }, []);

  const validateUsername = (username: string) => {
    if (!username.trim()) {
      return 'Username is required';
    }
    
    if (username.length < 3) {
      return 'Username must be at least 3 characters long';
    }
    
    if (username.length > 20) {
      return 'Username must be less than 20 characters long';
    }
    
    // Only allow alphanumeric characters and underscores
    if (!/^[a-zA-Z0-9_]+$/.test(username)) {
      return 'Username can only contain letters, numbers, and underscores';
    }
    
    return '';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Clear previous errors
    setError('');
    
    // Validate username
    const validationError = validateUsername(username);
    if (validationError) {
      setError(validationError);
      return;
    }
    
    // Check server status before proceeding
    setLoading(true);
    const isOnline = await healthApi.checkHealth();
    
    if (!isOnline) {
      setError('Chat server is currently unavailable. Please try again later.');
      setLoading(false);
      return;
    }
    
    // Store username in session storage
    sessionStorage.setItem('chatUsername', username.trim());
    
    // Navigate to chat room
    setLoading(false);
    router.push('/chat');
  };

  return (
    <div className="form-container">
      <h1 className="title">NextJS + .NET Core Chatroom</h1>
      <h2 className="subtitle">Join the conversation</h2>
      
      {!isServerOnline && (
        <ErrorMessage message="Chat server is offline. Please try again later." />
      )}
      
      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="username">Username</label>
          <input
            type="text"
            id="username"
            value={username}
            onChange={(e) => {
              setUsername(e.target.value);
              setError('');
            }}
            placeholder="Enter your username"
            disabled={loading || !isServerOnline}
            maxLength={20}
            aria-invalid={!!error}
            aria-describedby={error ? "username-error" : undefined}
          />
          {error && <p className="error-message" id="username-error">{error}</p>}
        </div>
        
        <button 
          type="submit" 
          className="form-button"
          disabled={loading || !isServerOnline || !username.trim()}
        >
          {loading ? 'Connecting...' : 'Join Chat'}
        </button>
      </form>
      
      <p className="instructions">
        Enter a username to join the chatroom. Your username must be 3-20 characters long
        and can only contain letters, numbers, and underscores.
      </p>
    </div>
  );
}