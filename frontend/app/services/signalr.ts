import { HubConnection, HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';

// Define SignalR hub URL from environment variables
const SOCKET_URL = process.env.NEXT_PUBLIC_SOCKET_URL || 'http://localhost:5000';
const HUB_URL = `${SOCKET_URL}/chatHub`;

// Message interface that matches the backend
export interface Message {
  id: string;
  text: string;
  username: string;
  timestamp: string;
  isCurrentUser?: boolean;
  isEdited?: boolean;
  editedAt?: string;
}

// Typing status
export interface TypingStatus {
  username: string;
  isTyping: boolean;
}

// Message edited data
export interface MessageEdited {
  messageId: string;
  text: string;
  editedAt: string;
}

// Message deleted data
export interface MessageDeleted {
  messageId: string;
}

// Message reactions
export interface Reaction {
  emoji: string;
  count: number;
  usernames: string[];
}

export interface ReactionsMap {
  [emoji: string]: Reaction;
}

export interface MessageReactions {
  messageId: string;
  reactions: ReactionsMap;
}

// Connection status type
export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected';

// Reconnection configuration
const RECONNECT_INTERVALS = [0, 2000, 5000, 10000, 15000, 30000]; // Increasing intervals in milliseconds
const MAX_RECONNECT_ATTEMPTS = RECONNECT_INTERVALS.length;
const TYPING_TIMEOUT = Number(process.env.NEXT_PUBLIC_TYPING_TIMEOUT) || 3000; // Typing timeout from env or default

// Chat connection class to manage SignalR connection
class ChatConnection {
  private connection: HubConnection | null = null;
  private messageListeners: ((message: Message) => void)[] = [];
  private typingListeners: ((status: TypingStatus) => void)[] = [];
  private connectionStatusListeners: ((status: ConnectionStatus) => void)[] = [];
  private activeUsersListeners: ((users: string[]) => void)[] = [];
  private messageEditedListeners: ((data: MessageEdited) => void)[] = [];
  private messageDeletedListeners: ((data: MessageDeleted) => void)[] = [];
  private messageReactionsListeners: ((data: MessageReactions) => void)[] = [];
  private reconnectAttempt: number = 0;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private currentUsername: string = '';
  private typingTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private isCurrentlyTyping: boolean = false;
  private sessionId: string = '';
  private connectionStatus: ConnectionStatus = 'disconnected';
  
  constructor() {
    // Generate a unique session ID for this browser tab
    this.sessionId = this.generateSessionId();
    
    // Handle browser's online/offline events
    if (typeof window !== 'undefined') {
      window.addEventListener('online', this.handleOnline);
      window.addEventListener('offline', this.handleOffline);
      
      // Handle page visibility changes to detect when tab is hidden/shown
      document.addEventListener('visibilitychange', this.handleVisibilityChange);
    }
  }
  
  // Generate a unique session ID 
  private generateSessionId(): string {
    return 'session_' + Math.random().toString(36).substring(2, 15) + 
           Math.random().toString(36).substring(2, 15) + 
           '_' + Date.now().toString();
  }
  
  // Handle when browser goes online
  private handleOnline = () => {
    console.log('Browser went online');
    if (this.connectionStatus === 'disconnected' && this.currentUsername) {
      this.start(this.currentUsername).catch(console.error);
    }
  }
  
  // Handle when browser goes offline
  private handleOffline = () => {
    console.log('Browser went offline');
    this.updateConnectionStatus('disconnected');
  }
  
  // Handle visibility change (tab switch)
  private handleVisibilityChange = () => {
    if (document.visibilityState === 'visible') {
      console.log('Tab became visible');
      // If we're disconnected, try to reconnect
      if (this.connectionStatus === 'disconnected' && this.currentUsername) {
        this.start(this.currentUsername).catch(console.error);
      }
    }
  }
  
  // Update connection status and notify listeners
  private updateConnectionStatus(status: ConnectionStatus) {
    if (this.connectionStatus !== status) {
      this.connectionStatus = status;
      this.notifyConnectionStatusListeners(status);
    }
  }
  
  // Initialize the connection
  async start(username: string): Promise<void> {
    try {
      // Update status to connecting
      this.updateConnectionStatus('connecting');
      
      // Clear any existing connection first
      if (this.connection) {
        await this.stop();
      }
      
      this.currentUsername = username;
      
      // Build the connection with more resilient options
      this.connection = new HubConnectionBuilder()
        .withUrl(HUB_URL, {
          skipNegotiation: false,
          transport: HttpTransportType.WebSockets,
          headers: {
            'X-Session-Id': this.sessionId // Add session ID to headers for server tracking
          }
        })
        .withAutomaticReconnect(RECONNECT_INTERVALS)
        .configureLogging(LogLevel.Information)
        .build();
      
      // Set up listeners
      this.setupListeners();
      
      // Start the connection
      await this.connection.start();
      console.log('SignalR connection established');
      
      // Reset reconnect attempt counter on successful connection
      this.reconnectAttempt = 0;
      
      // Join the chat room
      const joinResponse = await this.connection.invoke('JoinRoom', { 
        username,
        sessionId: this.sessionId // Send session ID in join request
      });
      
      if (!joinResponse.success) {
        throw new Error(joinResponse.error || 'Failed to join chat room');
      }
      
      // Update connection status to connected
      this.updateConnectionStatus('connected');
    } catch (error) {
      console.error('Error establishing SignalR connection:', error);
      this.updateConnectionStatus('disconnected');
      
      // Implement graceful retry logic
      this.handleConnectionError();
      throw error;
    }
  }
  
  // Handle connection errors with exponential backoff
  private handleConnectionError(): void {
    this.clearReconnectTimer();
    
    if (this.reconnectAttempt < MAX_RECONNECT_ATTEMPTS) {
      const delay = RECONNECT_INTERVALS[this.reconnectAttempt];
      console.log(`Attempting to reconnect in ${delay}ms (attempt ${this.reconnectAttempt + 1})`);
      
      this.reconnectTimer = setTimeout(() => {
        if (this.currentUsername) {
          this.start(this.currentUsername).catch(() => {
            this.reconnectAttempt++;
            this.handleConnectionError();
          });
        }
      }, delay);
    } else {
      console.error('Maximum reconnection attempts reached');
      // After max attempts, try again after a longer delay (30 seconds)
      this.reconnectTimer = setTimeout(() => {
        this.reconnectAttempt = 0; // Reset counter
        if (this.currentUsername) {
          this.start(this.currentUsername).catch(() => {
            this.handleConnectionError();
          });
        }
      }, 30000);
    }
  }
  
  // Clear reconnect timer
  private clearReconnectTimer(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
  }
  
  // Clear typing timeout
  private clearTypingTimeout(): void {
    if (this.typingTimeoutId) {
      clearTimeout(this.typingTimeoutId);
      this.typingTimeoutId = null;
    }
  }
  
  // Clean up resources on destroy
  destroy(): void {
    this.stop().catch(console.error);
    
    if (typeof window !== 'undefined') {
      window.removeEventListener('online', this.handleOnline);
      window.removeEventListener('offline', this.handleOffline);
      document.removeEventListener('visibilitychange', this.handleVisibilityChange);
    }
  }
  
  // Stop the connection
  async stop(): Promise<void> {
    // Clear all timers
    this.clearReconnectTimer();
    this.clearTypingTimeout();
    
    // Reset typing status if active
    if (this.isCurrentlyTyping && this.currentUsername) {
      try {
        // Try to send that user stopped typing
        if (this.connection && this.connection.state === 'Connected') {
          await this.connection.invoke('SendTypingStatus', { 
            username: this.currentUsername, 
            isTyping: false 
          });
        }
      } catch (err) {
        console.error('Error clearing typing status on disconnect:', err);
      }
      this.isCurrentlyTyping = false;
    }
    
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('SignalR connection stopped');
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      } finally {
        this.connection = null;
        this.currentUsername = '';
        this.updateConnectionStatus('disconnected');
      }
    }
  }
  
  // Send a message
  async sendMessage(text: string, username: string): Promise<void> {
    if (!this.connection || this.connectionStatus !== 'connected') {
      throw new Error('No active SignalR connection');
    }
    
    try {
      // Clear typing status when sending a message
      if (this.isCurrentlyTyping) {
        this.clearTypingTimeout();
        this.isCurrentlyTyping = false;
        await this.sendTypingStatus(username, false);
      }
      
      const response = await this.connection.invoke('SendMessage', { 
        text, 
        username,
        sessionId: this.sessionId // Include session ID
      });
      
      if (!response.success) {
        throw new Error(response.error || 'Failed to send message');
      }
    } catch (error) {
      console.error('Error sending message:', error);
      
      // If connection lost while sending, try to reconnect
      if (this.connection.state !== 'Connected') {
        this.updateConnectionStatus('disconnected');
        this.handleConnectionError();
      }
      
      throw error;
    }
  }
  
  // Send typing status
  async sendTypingStatus(username: string, isTyping: boolean): Promise<void> {
    if (!this.connection || this.connectionStatus !== 'connected') {
      // Silently fail if not connected
      return;
    }
    
    try {
      // Only send if status changed
      if (isTyping !== this.isCurrentlyTyping) {
        await this.connection.invoke('SendTypingStatus', { 
          username, 
          isTyping,
          sessionId: this.sessionId
        });
        this.isCurrentlyTyping = isTyping;
      }
      
      // Set a timeout to automatically clear typing status
      if (isTyping) {
        this.clearTypingTimeout();
        this.typingTimeoutId = setTimeout(async () => {
          if (this.isCurrentlyTyping) {
            this.isCurrentlyTyping = false;
            await this.sendTypingStatus(username, false);
          }
        }, TYPING_TIMEOUT);
      } else {
        this.clearTypingTimeout();
      }
    } catch (error) {
      console.error('Error sending typing status:', error);
      // Don't throw here to avoid interrupting normal flow
    }
  }
  
  // Edit a message
  async editMessage(messageId: string, text: string, username: string): Promise<void> {
    if (!this.connection || this.connectionStatus !== 'connected') {
      throw new Error('No active SignalR connection');
    }
    
    try {
      const response = await this.connection.invoke('EditMessage', { 
        messageId, 
        text, 
        username,
        sessionId: this.sessionId
      });
      
      if (!response.success) {
        throw new Error(response.error || 'Failed to edit message');
      }
    } catch (error) {
      console.error('Error editing message:', error);
      
      if (this.connection.state !== 'Connected') {
        this.updateConnectionStatus('disconnected');
        this.handleConnectionError();
      }
      
      throw error;
    }
  }
  
  // Delete a message
  async deleteMessage(messageId: string, username: string): Promise<void> {
    if (!this.connection || this.connectionStatus !== 'connected') {
      throw new Error('No active SignalR connection');
    }
    
    try {
      const response = await this.connection.invoke('DeleteMessage', { 
        messageId, 
        username,
        sessionId: this.sessionId
      });
      
      if (!response.success) {
        throw new Error(response.error || 'Failed to delete message');
      }
    } catch (error) {
      console.error('Error deleting message:', error);
      
      if (this.connection.state !== 'Connected') {
        this.updateConnectionStatus('disconnected');
        this.handleConnectionError();
      }
      
      throw error;
    }
  }
  
  // Add a reaction to a message
  async addReaction(messageId: string, emoji: string, username: string): Promise<void> {
    if (!this.connection || this.connectionStatus !== 'connected') {
      throw new Error('No active SignalR connection');
    }
    
    try {
      const response = await this.connection.invoke('AddReaction', { 
        messageId, 
        emoji, 
        username,
        sessionId: this.sessionId
      });
      
      if (!response.success) {
        throw new Error(response.error || 'Failed to add reaction');
      }
    } catch (error) {
      console.error('Error adding reaction:', error);
      
      if (this.connection.state !== 'Connected') {
        this.updateConnectionStatus('disconnected');
        this.handleConnectionError();
      }
      
      throw error;
    }
  }
  
  // Remove a reaction from a message
  async removeReaction(messageId: string, emoji: string, username: string): Promise<void> {
    if (!this.connection || this.connectionStatus !== 'connected') {
      throw new Error('No active SignalR connection');
    }
    
    try {
      const response = await this.connection.invoke('RemoveReaction', { 
        messageId, 
        emoji, 
        username,
        sessionId: this.sessionId
      });
      
      if (!response.success) {
        throw new Error(response.error || 'Failed to remove reaction');
      }
    } catch (error) {
      console.error('Error removing reaction:', error);
      
      if (this.connection.state !== 'Connected') {
        this.updateConnectionStatus('disconnected');
        this.handleConnectionError();
      }
      
      throw error;
    }
  }
  
  // Get reactions for a message
  async getReactions(messageId: string): Promise<ReactionsMap> {
    if (!this.connection || this.connectionStatus !== 'connected') {
      throw new Error('No active SignalR connection');
    }
    
    try {
      const response = await this.connection.invoke('GetReactions', { 
        messageId,
        sessionId: this.sessionId
      });
      
      if (!response.success) {
        throw new Error(response.error || 'Failed to get reactions');
      }
      
      return response.reactions || {};
    } catch (error) {
      console.error('Error getting reactions:', error);
      
      if (this.connection.state !== 'Connected') {
        this.updateConnectionStatus('disconnected');
        this.handleConnectionError();
      }
      
      throw error;
    }
  }
  
  // Set up connection event listeners
  private setupListeners(): void {
    if (!this.connection) return;
    
    // Message listener
    this.connection.on('message', (message: Message) => {
      this.notifyMessageListeners(message);
    });
    
    // Typing status listener
    this.connection.on('typingStatus', (status: TypingStatus) => {
      this.notifyTypingListeners(status);
    });
    
    // Active users listener
    this.connection.on('activeUsers', (users: string[]) => {
      this.notifyActiveUsersListeners(users);
    });
    
    // Message edited listener
    this.connection.on('messageEdited', (data: MessageEdited) => {
      this.notifyMessageEditedListeners(data);
    });
    
    // Message deleted listener
    this.connection.on('messageDeleted', (data: MessageDeleted) => {
      this.notifyMessageDeletedListeners(data);
    });
    
    // Message reactions listener
    this.connection.on('messageReactions', (data: MessageReactions) => {
      this.notifyMessageReactionsListeners(data);
    });
    
    // Server forces disconnect (e.g., duplicate login)
    this.connection.on('disconnected', (reason: string) => {
      console.log('Server requested disconnect:', reason);
      this.stop().catch(console.error);
    });
    
    // Connection events
    this.connection.onreconnecting((error) => {
      console.log('SignalR attempting to reconnect...', error);
      this.updateConnectionStatus('connecting');
    });
    
    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
      
      // If we have a username, rejoin the room after reconnection
      if (this.currentUsername) {
        this.connection?.invoke('JoinRoom', { 
          username: this.currentUsername,
          sessionId: this.sessionId
        })
        .catch(err => console.error('Error rejoining after reconnect:', err));
      }
      
      this.updateConnectionStatus('connected');
      this.reconnectAttempt = 0;
    });
    
    this.connection.onclose((error) => {
      console.log('SignalR connection closed', error);
      this.updateConnectionStatus('disconnected');
      
      // Try to reconnect if closed unexpectedly and we have a username
      if (this.currentUsername) {
        this.handleConnectionError();
      }
    });
  }
  
  // Register message listener
  onMessage(callback: (message: Message) => void): () => void {
    this.messageListeners.push(callback);
    
    // Return unsubscribe function
    return () => {
      this.messageListeners = this.messageListeners.filter(listener => listener !== callback);
    };
  }
  
  // Register typing status listener
  onTypingStatus(callback: (status: TypingStatus) => void): () => void {
    this.typingListeners.push(callback);
    
    // Return unsubscribe function
    return () => {
      this.typingListeners = this.typingListeners.filter(listener => listener !== callback);
    };
  }
  
  // Register connection status listener
  onConnectionChange(callback: (status: ConnectionStatus) => void): () => void {
    this.connectionStatusListeners.push(callback);
    
    // If already connected, immediately notify with current status
    callback(this.connectionStatus);
    
    // Return unsubscribe function
    return () => {
      this.connectionStatusListeners = this.connectionStatusListeners.filter(listener => listener !== callback);
    };
  }
  
  // Register active users listener
  onActiveUsers(callback: (users: string[]) => void): () => void {
    this.activeUsersListeners.push(callback);
    
    // Return unsubscribe function
    return () => {
      this.activeUsersListeners = this.activeUsersListeners.filter(listener => listener !== callback);
    };
  }
  
  // Register message edited listener
  onMessageEdited(callback: (data: MessageEdited) => void): () => void {
    this.messageEditedListeners.push(callback);
    
    // Return unsubscribe function
    return () => {
      this.messageEditedListeners = this.messageEditedListeners.filter(listener => listener !== callback);
    };
  }
  
  // Register message deleted listener
  onMessageDeleted(callback: (data: MessageDeleted) => void): () => void {
    this.messageDeletedListeners.push(callback);
    
    // Return unsubscribe function
    return () => {
      this.messageDeletedListeners = this.messageDeletedListeners.filter(listener => listener !== callback);
    };
  }
  
  // Register message reactions listener
  onMessageReactions(callback: (data: MessageReactions) => void): () => void {
    this.messageReactionsListeners.push(callback);
    
    // Return unsubscribe function
    return () => {
      this.messageReactionsListeners = this.messageReactionsListeners.filter(listener => listener !== callback);
    };
  }
  
  // Notify all message listeners
  private notifyMessageListeners(message: Message): void {
    this.messageListeners.forEach(listener => listener(message));
  }
  
  // Notify all typing status listeners
  private notifyTypingListeners(status: TypingStatus): void {
    this.typingListeners.forEach(listener => listener(status));
  }
  
  // Notify all connection status listeners
  private notifyConnectionStatusListeners(status: ConnectionStatus): void {
    this.connectionStatusListeners.forEach(listener => listener(status));
  }
  
  // Notify all active users listeners
  private notifyActiveUsersListeners(users: string[]): void {
    this.activeUsersListeners.forEach(listener => listener(users));
  }
  
  // Notify all message edited listeners
  private notifyMessageEditedListeners(data: MessageEdited): void {
    this.messageEditedListeners.forEach(listener => listener(data));
  }
  
  // Notify all message deleted listeners
  private notifyMessageDeletedListeners(data: MessageDeleted): void {
    this.messageDeletedListeners.forEach(listener => listener(data));
  }
  
  // Notify all message reactions listeners
  private notifyMessageReactionsListeners(data: MessageReactions): void {
    this.messageReactionsListeners.forEach(listener => listener(data));
  }
  
  // Get current connection status
  getConnectionStatus(): ConnectionStatus {
    return this.connectionStatus;
  }
}

// Export singleton instance
export const chatConnection = new ChatConnection();