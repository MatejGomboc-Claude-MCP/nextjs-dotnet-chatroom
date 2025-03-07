import React from 'react';

interface UsersListProps {
  users: string[];
  currentUser: string;
}

const UsersList: React.FC<UsersListProps> = ({ users, currentUser }) => {
  // Sort users alphabetically but with current user first
  const sortedUsers = [...users].sort((a, b) => {
    if (a === currentUser) return -1;
    if (b === currentUser) return 1;
    return a.localeCompare(b);
  });
  
  return (
    <div className="users-list">
      <h3 className="users-list-title">Active Users ({users.length})</h3>
      <ul className="users-list-items">
        {sortedUsers.map((user) => (
          <li key={user} className="user-item">
            <div className="user-avatar">
              {user.charAt(0).toUpperCase()}
            </div>
            <span className="user-name">
              {user === currentUser ? `${user} (You)` : user}
            </span>
            <span className={`user-status ${user === currentUser ? 'user-status-you' : 'user-status-active'}`}></span>
          </li>
        ))}
      </ul>
    </div>
  );
};

export default UsersList;