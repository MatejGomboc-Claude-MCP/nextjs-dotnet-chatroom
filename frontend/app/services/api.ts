import axios, { AxiosError } from 'axios';

// Define the base API URL from environment variables
const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';
const BASE_URL = API_URL.endsWith('/api') ? API_URL.substring(0, API_URL.length - 4) : API_URL;

// Create axios instance with default config
const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // Add timeout to prevent long-pending requests
});

// Add request interceptor for potential auth tokens
apiClient.interceptors.request.use((config) => {
  // If we implement auth later, we can add token here
  // const token = localStorage.getItem('token');
  // if (token) {
  //   config.headers.Authorization = `Bearer ${token}`;
  // }
  return config;
});

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // Custom error handling
    if (error.response) {
      // Server responded with error status (4xx, 5xx)
      console.error('API Error:', error.response.data);
      
      // Handle specific status codes
      switch (error.response.status) {
        case 401:
          // Unauthorized - handle auth error
          console.error('Authentication required');
          break;
        case 403:
          // Forbidden - handle permission error
          console.error('Permission denied');
          break;
        case 404:
          // Not found
          console.error('Resource not found');
          break;
        case 429:
          // Rate limited
          console.error('Rate limit exceeded. Please try again later.');
          break;
        case 500:
          // Server error
          console.error('Server error occurred');
          break;
        default:
          console.error(`Error: ${error.response.status}`);
      }
    } else if (error.request) {
      // Request made but no response received
      console.error('No response received from server');
    } else {
      // Error in setting up the request
      console.error('Error configuring request:', error.message);
    }
    
    return Promise.reject(error);
  }
);

// Types
export interface Message {
  id: string;
  text: string;
  username: string;
  timestamp: string;
  isCurrentUser?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageCount: number;
  currentPage: number;
  pageSize: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface ApiError {
  message: string;
  status?: number;
  details?: any;
}

// Messages API
export const messagesApi = {
  getAllMessages: async () => {
    try {
      const response = await apiClient.get('/messages');
      return response.data;
    } catch (error) {
      console.error('Failed to fetch messages:', error);
      throw error;
    }
  },
  
  getMessagesPaged: async (page: number = 1, pageSize: number = 50) => {
    try {
      // Validate inputs
      const validPage = Math.max(1, page);
      const validPageSize = Math.min(100, Math.max(1, pageSize));
      
      const response = await apiClient.get<PagedResult<Message>>('/messages', {
        params: { 
          page: validPage, 
          pageSize: validPageSize 
        }
      });
      return response.data;
    } catch (error) {
      console.error('Failed to fetch paged messages:', error);
      throw error;
    }
  },
  
  getMessage: async (id: string) => {
    try {
      if (!id || !id.trim()) {
        throw new Error('Message ID is required');
      }
      
      const response = await apiClient.get(`/messages/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Failed to fetch message with ID ${id}:`, error);
      throw error;
    }
  },
  
  createMessage: async (text: string, username: string) => {
    try {
      if (!text.trim()) {
        throw new Error('Message text is required');
      }
      
      if (!username.trim()) {
        throw new Error('Username is required');
      }
      
      // Sanitize inputs to prevent injection attacks
      const sanitizedData = {
        text: text.trim(),
        username: username.trim()
      };
      
      const response = await apiClient.post('/messages', sanitizedData);
      return response.data;
    } catch (error) {
      console.error('Failed to create message:', error);
      throw error;
    }
  }
};

// API Utilities
export const handleApiError = (error: any): ApiError => {
  if (axios.isAxiosError(error)) {
    const axiosError = error as AxiosError<any>;
    return {
      message: axiosError.response?.data?.message || axiosError.message,
      status: axiosError.response?.status,
      details: axiosError.response?.data
    };
  }
  return { 
    message: error instanceof Error ? error.message : 'An unknown error occurred' 
  };
};

// Health API
export const healthApi = {
  checkHealth: async () => {
    try {
      // Use the base URL with health endpoint
      const response = await axios.get(`${BASE_URL}/health`);
      return response.status === 200;
    } catch (error) {
      console.error('Health check failed:', error);
      return false;
    }
  }
};

export default apiClient;