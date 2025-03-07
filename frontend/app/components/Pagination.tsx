import React from 'react';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  className?: string;
  disabled?: boolean;
}

const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  onPageChange,
  className = '',
  disabled = false,
}) => {
  // Don't show pagination if there's only one page
  if (totalPages <= 1) {
    return null;
  }

  // Create an array of page numbers to display
  const getPageNumbers = () => {
    const pages = [];
    const maxPagesToShow = 5;
    
    if (totalPages <= maxPagesToShow) {
      // If total pages is less than max pages to show, show all pages
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Always show first page
      pages.push(1);
      
      // Calculate start and end of pages to show
      let start = Math.max(2, currentPage - 1);
      let end = Math.min(totalPages - 1, start + 2);
      
      // Adjust start if end is too close to the total pages
      if (end === totalPages - 1) {
        start = Math.max(2, end - 2);
      }
      
      // Add ellipsis if needed before middle pages
      if (start > 2) {
        pages.push(-1); // -1 represents ellipsis
      }
      
      // Add middle pages
      for (let i = start; i <= end; i++) {
        pages.push(i);
      }
      
      // Add ellipsis if needed after middle pages
      if (end < totalPages - 1) {
        pages.push(-2); // -2 represents ellipsis
      }
      
      // Always show last page
      pages.push(totalPages);
    }
    
    return pages;
  };

  // Handle page change with disabled state check
  const handlePageChange = (page: number) => {
    if (!disabled && page !== currentPage) {
      onPageChange(page);
    }
  };

  return (
    <div className={`pagination ${className} ${disabled ? 'disabled' : ''}`}>
      <button
        className="pagination-button previous"
        onClick={() => handlePageChange(currentPage - 1)}
        disabled={currentPage === 1 || disabled}
        aria-label="Previous page"
      >
        &laquo; Previous
      </button>
      
      <div className="pagination-pages">
        {getPageNumbers().map((page, index) => {
          if (page < 0) {
            // Render ellipsis
            return <span key={`ellipsis-${index}`} className="pagination-ellipsis">...</span>;
          }
          
          return (
            <button
              key={page}
              className={`pagination-button ${page === currentPage ? 'active' : ''}`}
              onClick={() => handlePageChange(page)}
              disabled={page === currentPage || disabled}
              aria-label={`Page ${page}`}
              aria-current={page === currentPage ? 'page' : undefined}
            >
              {page}
            </button>
          );
        })}
      </div>
      
      <button
        className="pagination-button next"
        onClick={() => handlePageChange(currentPage + 1)}
        disabled={currentPage === totalPages || disabled}
        aria-label="Next page"
      >
        Next &raquo;
      </button>
    </div>
  );
};

export default Pagination;
