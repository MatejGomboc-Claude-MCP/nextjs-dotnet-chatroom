import React from 'react';

interface ErrorMessageProps {
  message: string;
  onRetry?: () => void;
}

const ErrorMessage: React.FC<ErrorMessageProps> = ({ message, onRetry }) => {
  return (
    <div className="error-container" role="alert" aria-live="assertive">
      <div className="error-icon">⚠️</div>
      <p className="error-text">{message}</p>
      {onRetry && (
        <button 
          onClick={onRetry}
          className="error-retry-button"
          aria-label="Retry"
        >
          Try Again
        </button>
      )}
    </div>
  );
};

export default ErrorMessage;
