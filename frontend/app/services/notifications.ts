// Service for handling browser notifications
const NOTIFICATION_TITLE = 'New Message';
const NOTIFICATION_ICON = '/notification-icon.png'; // Path to notification icon

export interface NotificationOptions {
  body: string;
  icon?: string;
  tag?: string;
  onClick?: () => void;
}

export const notificationService = {
  // Check if notifications are supported
  isSupported: (): boolean => {
    return 'Notification' in window;
  },
  
  // Request permission for notifications
  requestPermission: async (): Promise<boolean> => {
    if (!notificationService.isSupported()) {
      return false;
    }
    
    try {
      const permission = await Notification.requestPermission();
      return permission === 'granted';
    } catch (error) {
      console.error('Error requesting notification permission:', error);
      return false;
    }
  },
  
  // Check if permission is granted
  hasPermission: (): boolean => {
    if (!notificationService.isSupported()) {
      return false;
    }
    
    return Notification.permission === 'granted';
  },
  
  // Show a notification
  showNotification: (message: string, options: NotificationOptions = { body: '' }): void => {
    if (!notificationService.isSupported() || !notificationService.hasPermission()) {
      return;
    }
    
    try {
      const notification = new Notification(NOTIFICATION_TITLE, {
        body: message,
        icon: options.icon || NOTIFICATION_ICON,
        tag: options.tag || 'chatroom-notification',
      });
      
      if (options.onClick) {
        notification.onclick = () => {
          window.focus();
          notification.close();
          options.onClick && options.onClick();
        };
      }
    } catch (error) {
      console.error('Error showing notification:', error);
    }
  }
};

export default notificationService;