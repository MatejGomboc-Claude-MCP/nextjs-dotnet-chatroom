#!/bin/bash

# Deployment script for Chatroom application on Debian 12
# Run this script as root or with sudo privileges

set -e  # Exit immediately if a command exits with a non-zero status

echo "Starting deployment of Chatroom application..."

# Ensure system is up to date
echo "Updating system packages..."
apt update && apt upgrade -y

# Check if required directories exist, create if not
if [ ! -d "/var/www/chatroom" ]; then
  echo "Creating application directories..."
  mkdir -p /var/www/chatroom/frontend
  mkdir -p /var/www/chatroom/backend
fi

# Deploy frontend
echo "Deploying NextJS frontend..."
cd /path/to/repo/frontend
npm install
npm run build

echo "Copying frontend files to /var/www/chatroom/frontend..."
cp -R .next /var/www/chatroom/frontend/
cp -R public /var/www/chatroom/frontend/
cp next.config.js package.json /var/www/chatroom/frontend/

# Deploy backend
echo "Deploying .NET Core backend..."
cd /path/to/repo/backend
dotnet publish -c Release -o /var/www/chatroom/backend

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

[Install]
WantedBy=multi-user.target
EOL
fi

# Configure Nginx
if [ ! -f "/etc/nginx/sites-available/chatroom" ]; then
  echo "Configuring Nginx..."
  cat > /etc/nginx/sites-available/chatroom << EOL
server {
    listen 80;
    server_name your_domain_or_ip;

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
echo "Application should be available at http://your_domain_or_ip/"
