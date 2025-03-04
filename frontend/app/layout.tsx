import './styles/globals.scss';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Chatroom App',
  description: 'Simple chatroom application with NextJS and .NET Core',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>
        <div className="app-container">
          <header className="app-header">
            <h1>Chatroom App</h1>
          </header>
          <main className="app-main">{children}</main>
          <footer className="app-footer">
            <p>&copy; {new Date().getFullYear()} Chatroom App</p>
          </footer>
        </div>
      </body>
    </html>
  );
}
