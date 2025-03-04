#!/bin/bash

# Deployment script for Chatroom application on Debian 12
# Run this script as root or with sudo privileges

set -e  # Exit immediately if a command exits with a non-zero status

echo "Starting deployment of Chatroom application..."

# Check if required variables are set
if [ -z "$REPO_PATH" ]; then
  echo "Error: REPO_PATH environment variable is not set."
  echo "Example: export REPO_PATH=/path/to/your/repo"
  exit 1
fi

if [ -z "$DOMAIN" ]; then
  echo "Warning: DOMAIN environment variable is not set. Using 'localhost' as default."
  DOMAIN="localhost"
fi

if [ -z "$MARIADB_PASSWORD" ]; then
  echo "Error: MARIADB_PASSWORD environment variable is not set."
  echo "Example: export MARIADB_PASSWORD=your_secure_password"
  exit 1
fi

if [ -z "$DB_USER" ]; then
  echo "Warning: DB_USER environment variable is not set. Using 'chatroomuser' as default."
  DB_USER="chatroomuser"
fi

if [ -z "$DB_NAME" ]; then
  echo "Warning: DB_NAME environment variable is not set. Using 'chatroom' as default."
  DB_NAME="chatroom"
fi

# Ensure system is up to date
echo "Updating system packages..."
apt update && apt upgrade -y

# Check if MariaDB is installed, install if not
if ! dpkg -l | grep -q mariadb-server; then
  echo "Installing MariaDB..."
  apt install -y mariadb-server
  systemctl enable mariadb
  systemctl start mariadb
  mysql_secure_installation
fi

# Check if Nginx is installed, install if not
if ! dpkg -l | grep -q nginx; then
  echo "Installing Nginx..."
  apt install -y nginx
  systemctl enable nginx
  systemctl start nginx
fi

# Check if required directories exist, create if not
if [ ! -d "/var/www/chatroom" ]; then
  echo "Creating application directories..."
  mkdir -p /var/www/chatroom/frontend
  mkdir -p /var/www/chatroom/backend
fi

# Deploy frontend
echo "Deploying NextJS frontend..."
cd $REPO_PATH/frontend
npm install
npm run build

echo "Copying frontend files to /var/www/chatroom/frontend..."
cp -R .next /var/www/chatroom/frontend/
cp -R public /var/www/chatroom/frontend/
cp next.config.js package.json /var/www/chatroom/frontend/

# Create .env.local for frontend
cat > /var/www/chatroom/frontend/.env.local << EOL
NEXT_PUBLIC_API_URL=http://$DOMAIN/api
NEXT_PUBLIC_SOCKET_URL=http://$DOMAIN
EOL

# Deploy backend
echo "Deploying .NET Core backend..."
cd $REPO_PATH/backend
dotnet publish -c Release -o /var/www/chatroom/backend

# Set up database
echo "Setting up database..."
# Check if database exists
if ! mysql -e "USE $DB_NAME" 2>/dev/null; then
  echo "Creating database and user..."
  mysql -e "CREATE DATABASE $DB_NAME;"
  mysql -e "CREATE USER IF NOT EXISTS '$DB_USER'@'localhost' IDENTIFIED BY '$MARIADB_PASSWORD';"
  mysql -e "GRANT ALL PRIVILEGES ON $DB_NAME.* TO '$DB_USER'@'localhost';"
  mysql -e "FLUSH PRIVILEGES;"
fi

# Create service files if they don't exist
if [ ! -f "/etc/systemd/system/chatroom-frontend.service" ]; then
  echo "Creating frontend service..."
  cat > /etc/systemd/system/chatroom-frontend.service << EOL
[Unit]
Description=Chatroom Frontend Service
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/var/www/chatroom/frontend
ExecStart=/usr/bin/npm start
Restart=on-failure
Environment="NODE_ENV=production"

[Install]
WantedBy=multi-user.target
EOL
fi

if [ ! -f "/etc/systemd/system/chatroom-backend.service" ]; then
  echo "Creating backend service..."
  cat > /etc/systemd/system/chatroom-backend.service << EOL
[Unit]
Description=Chatroom Backend Service
After=network.target mariadb.service

[Service]
Type=simple
User=www-data
WorkingDirectory=/var/www/chatroom/backend
ExecStart=/usr/bin/dotnet ChatRoom.Api.dll
Restart=on-failure
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=MARIADB_PASSWORD=$MARIADB_PASSWORD

[Install]
WantedBy=multi-user.target
EOL
fi

# Configure Nginx with HTTPS
if [ ! -f "/etc/nginx/sites-available/chatroom" ]; then
  echo "Configuring Nginx..."
  cat > /etc/nginx/sites-available/chatroom << EOL
server {
    listen 80;
    server_name $DOMAIN;

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
    }

    location /api {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
    }

    location /chatHub {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
    }
}
EOL

  # Enable the site
  ln -sf /etc/nginx/sites-available/chatroom /etc/nginx/sites-enabled/
  systemctl restart nginx
fi

# Set proper permissions
echo "Setting proper permissions..."
chown -R www-data:www-data /var/www/chatroom

# Restart or start services
echo "Starting services..."
systemctl daemon-reload
systemctl enable chatroom-frontend.service
systemctl enable chatroom-backend.service
systemctl restart chatroom-frontend.service
systemctl restart chatroom-backend.service

echo "Deployment completed successfully!"
echo "Application should be available at http://$DOMAIN/"
echo "Consider setting up HTTPS using Let's Encrypt for production use."
