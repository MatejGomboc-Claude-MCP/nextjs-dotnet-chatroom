import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

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
}

// Typing status
export interface TypingStatus {
  username: string;
  isTyping: boolean;
}

// Chat connection class to manage SignalR connection
class ChatConnection {
  private connection: HubConnection | null = null;
  private messageListeners: ((message: Message) => void)[] = [];
  private typingListeners: ((status: TypingStatus) => void)[] = [];
  private connectionListeners: ((isConnected: boolean) => void)[] = [];
  
  // Initialize the connection
  async start(username: string): Promise<void> {
    try {
      // Build the connection
      this.connection = new HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();
      
      // Set up listeners
      this.setupListeners();
      
      // Start the connection
      await this.connection.start();
      console.log('SignalR connection established');
      
      // Join the chat room
      await this.connection.invoke('JoinRoom', { username });
      
      // Notify listeners that connection is established
      this.notifyConnectionListeners(true);
    } catch (error) {
      console.error('Error establishing SignalR connection:', error);
      this.notifyConnectionListeners(false);
      throw error;
    }
  }
  
  // Stop the connection
  async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('SignalR connection stopped');
        this.notifyConnectionListeners(false);
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      } finally {
        this.connection = null;
      }
    }
  }
  
  // Send a message
  async sendMessage(text: string, username: string): Promise<void> {
    if (!this.connection) {
      throw new Error('No active SignalR connection');
    }
    
    try {
      await this.connection.invoke('SendMessage', { text, username });
    } catch (error) {
      console.error('Error sending message:', error);
      throw error;
    }
  }
  
  // Send typing status
  async sendTypingStatus(username: string, isTyping: boolean): Promise<void> {
    if (!this.connection) {
      throw new Error('No active SignalR connection');
    }
    
    try {
      await this.connection.invoke('SendTypingStatus', { username, isTyping });
    } catch (error) {
      console.error('Error sending typing status:', error);
      // Don't throw here to avoid interrupting normal flow
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
    
    // Connection events
    this.connection.onreconnecting(() => {
      console.log('SignalR attempting to reconnect...');
      this.notifyConnectionListeners(false);
    });
    
    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.notifyConnectionListeners(true);
    });
    
    this.connection.onclose(() => {
      console.log('SignalR connection closed');
      this.notifyConnectionListeners(false);
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
  onConnectionChange(callback: (isConnected: boolean) => void): () => void {
    this.connectionListeners.push(callback);
    
    // Return unsubscribe function
    return () => {
      this.connectionListeners = this.connectionListeners.filter(listener => listener !== callback);
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
  
  // Notify all connection listeners
  private notifyConnectionListeners(isConnected: boolean): void {
    this.connectionListeners.forEach(listener => listener(isConnected));
  }
  
  // Check if connected
  isConnected(): boolean {
    return !!this.connection && this.connection.state === 'Connected';
  }
}

// Export singleton instance
export const chatConnection = new ChatConnection();
