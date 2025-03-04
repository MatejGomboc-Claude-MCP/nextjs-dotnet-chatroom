'use client';

import './styles/globals.scss';
import ErrorBoundary from './components/ErrorBoundary';

// Export a client component that wraps the children in an error boundary
export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <head>
        <title>Chatroom App</title>
        <meta name="description" content="Simple chatroom application with NextJS and .NET Core" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        {/* Added security headers */}
        <meta httpEquiv="X-Content-Type-Options" content="nosniff" />
        <meta httpEquiv="X-Frame-Options" content="DENY" />
        <meta httpEquiv="Content-Security-Policy" content="default-src 'self'; connect-src 'self' ws: wss:; img-src 'self' data:; style-src 'self' 'unsafe-inline';" />
      </head>
      <body>
        <div className="app-container">
          <header className="app-header">
            <h1>Chatroom App</h1>
          </header>
          <main className="app-main">
            <ErrorBoundary>
              {children}
            </ErrorBoundary>
          </main>
          <footer className="app-footer">
            <p>&copy; {new Date().getFullYear()} Chatroom App</p>
          </footer>
        </div>
      </body>
    </html>
  );
}