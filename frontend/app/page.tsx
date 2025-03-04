'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

export default function Home() {
  const [username, setUsername] = useState('');
  const [error, setError] = useState('');
  const router = useRouter();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!username.trim()) {
      setError('Username is required');
      return;
    }
    
    // Store username in session storage
    sessionStorage.setItem('chatUsername', username);
    
    // Navigate to chat room
    router.push('/chat');
  };

  return (
    <div className="form-container">
      <h2 style={{ marginBottom: '1.5rem', textAlign: 'center' }}>Join Chatroom</h2>
      
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
          />
          {error && <p className="error-message">{error}</p>}
        </div>
        
        <button type="submit" className="form-button">
          Join Chat
        </button>
      </form>
    </div>
  );
}
