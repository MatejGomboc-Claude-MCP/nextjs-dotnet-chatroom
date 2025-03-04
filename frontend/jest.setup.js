// Add Jest specific matchers for testing DOM elements
import '@testing-library/jest-dom';

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter() {
    return {
      push: jest.fn(),
      back: jest.fn(),
      forward: jest.fn(),
    };
  },
  usePathname() {
    return '/';
  },
}));

// Mock sessionsStorage (for tests)
const mockStorage = {};
Object.defineProperty(window, 'sessionStorage', {
  value: {
    getItem: (key) => mockStorage[key] ?? null,
    setItem: (key, value) => { mockStorage[key] = value; },
    removeItem: (key) => { delete mockStorage[key]; },
    clear: () => { Object.keys(mockStorage).forEach(key => delete mockStorage[key]); },
  },
});

// Mock window.scrollIntoView
window.HTMLElement.prototype.scrollIntoView = jest.fn();

// Mock ResizeObserver (required for some UI components)
global.ResizeObserver = jest.fn().mockImplementation(() => ({
  observe: jest.fn(),
  unobserve: jest.fn(),
  disconnect: jest.fn(),
}));

// Suppress console errors during tests
console.error = jest.fn();
