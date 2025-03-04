import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import MessageItem from './MessageItem';

// Mock DOMPurify to avoid issues in test environment
jest.mock('dompurify', () => ({
  sanitize: jest.fn((content) => content),
}));

describe('MessageItem', () => {
  const sentMessage = {
    id: '1',
    text: 'Hello world',
    username: 'TestUser',
    timestamp: '2023-01-01T12:00:00Z',
    isCurrentUser: true,
  };

  const receivedMessage = {
    id: '2',
    text: 'Reply message',
    username: 'OtherUser',
    timestamp: '2023-01-01T12:01:00Z',
    isCurrentUser: false,
  };

  test('renders sent message correctly', () => {
    render(<MessageItem message={sentMessage} />);
    
    // Check for message content
    expect(screen.getByText('Hello world')).toBeInTheDocument();
    
    // Check for username
    expect(screen.getByText('TestUser')).toBeInTheDocument();
    
    // Check message has sent class
    const messageElement = screen.getByText('Hello world').closest('.message');
    expect(messageElement).toHaveClass('sent');
  });

  test('renders received message correctly', () => {
    render(<MessageItem message={receivedMessage} />);
    
    // Check for message content
    expect(screen.getByText('Reply message')).toBeInTheDocument();
    
    // Check for username
    expect(screen.getByText('OtherUser')).toBeInTheDocument();
    
    // Check message has received class
    const messageElement = screen.getByText('Reply message').closest('.message');
    expect(messageElement).toHaveClass('received');
  });

  test('formats timestamp correctly', () => {
    render(<MessageItem message={sentMessage} />);
    
    // The actual formatted time would depend on the locale of the test environment
    // So we just check that some time string is present
    const timeElement = screen.getByText(/\d{1,2}:\d{2}/);
    expect(timeElement).toBeInTheDocument();
  });

  test('handles invalid timestamp gracefully', () => {
    const invalidTimestampMessage = {
      ...sentMessage,
      timestamp: 'invalid-date',
    };
    
    render(<MessageItem message={invalidTimestampMessage} />);
    
    // Check it falls back to a default message
    expect(screen.getByText('Invalid time')).toBeInTheDocument();
  });
});