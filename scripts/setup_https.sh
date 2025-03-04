#!/bin/bash

# HTTPS setup script for Chatroom application on Debian 12
# Run this script as root or with sudo privileges after the deploy.sh script

set -e  # Exit immediately if a command exits with a non-zero status

# Check if required variables are set
if [ -z "$DOMAIN" ]; then
  echo "Error: DOMAIN environment variable is not set."
  echo "Example: export DOMAIN=your-domain.com"
  exit 1
fi

# Check if certbot is installed, install if not
if ! dpkg -l | grep -q certbot; then
  echo "Installing Certbot..."
  apt update
  apt install -y certbot python3-certbot-nginx
fi

# Obtain SSL certificate with Certbot
echo "Obtaining SSL certificate for $DOMAIN..."
certbot --nginx -d $DOMAIN --non-interactive --agree-tos --email admin@$DOMAIN

# Update Nginx configuration to force HTTPS
echo "Configuring Nginx to force HTTPS..."
cat > /etc/nginx/sites-available/chatroom << EOL
server {
    listen 80;
    server_name $DOMAIN;
    return 301 https://$DOMAIN$request_uri;
}

server {
    listen 443 ssl;
    server_name $DOMAIN;

    ssl_certificate /etc/letsencrypt/live/$DOMAIN/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/$DOMAIN/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_prefer_server_ciphers on;
    ssl_ciphers "EECDH+AESGCM:EDH+AESGCM:AES256+EECDH:AES256+EDH";
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 1h;
    ssl_session_tickets off;
    ssl_stapling on;
    ssl_stapling_verify on;

    # Increase security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options nosniff;
    add_header X-Frame-Options SAMEORIGIN;
    add_header X-XSS-Protection "1; mode=block";

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location /api {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location /chatHub {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOL

# Restart Nginx to apply changes
systemctl restart nginx

# Setup auto-renewal for SSL certificate
echo "Setting up automatic renewal of SSL certificate..."
echo "0 0 * * * root certbot renew --quiet --nginx" > /etc/cron.d/certbot-renew

echo "HTTPS setup completed successfully!"
echo "Your application is now available at https://$DOMAIN/"
