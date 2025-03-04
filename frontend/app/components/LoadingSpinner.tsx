import React from 'react';

interface LoadingSpinnerProps {
  message?: string;
  size?: 'small' | 'medium' | 'large';
}

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ 
  message = 'Loading...', 
  size = 'medium' 
}) => {
  const sizeClass = {
    small: 'spinner-small',
    medium: 'spinner-medium',
    large: 'spinner-large',
  }[size];

  return (
    <div className={`loading-container ${sizeClass}`} role="status" aria-live="polite">
      <div className="spinner"></div>
      {message && <p className="loading-text">{message}</p>}
    </div>
  );
};

export default LoadingSpinner;
