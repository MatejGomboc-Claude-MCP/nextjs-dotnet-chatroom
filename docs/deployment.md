# Deployment Guide

This guide provides step-by-step instructions for deploying the chatroom application to a production environment on Debian Linux.

## Prerequisites

- Debian 12 (Bookworm) server
- Root or sudo access
- Domain name with DNS configured to point to your server
- Basic knowledge of Linux command line

## Server Preparation

### Update System Packages

```bash
sudo apt update
sudo apt upgrade -y
```

### Install Required Packages

```bash
sudo apt install -y curl wget gnupg2 apt-transport-https software-properties-common lsb-release ca-certificates unzip git
```

## Component Installation

### 1. Install Node.js

```bash
# Add NodeSource repository
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -

# Install Node.js
sudo apt install -y nodejs
```

Verify the installation:
```bash
node --version  # Should show v20.x.x
npm --version   # Should show v10.x.x or higher
```

### 2. Install .NET 8 SDK

```bash
# Add Microsoft repository
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt update
sudo apt install -y dotnet-sdk-8.0
```

Verify the installation:
```bash
dotnet --version  # Should show 8.0.x
```

### 3. Install MariaDB

```bash
# Add MariaDB repository
sudo apt install -y software-properties-common
sudo curl -LsS -O https://downloads.mariadb.com/MariaDB/mariadb_repo_setup
sudo bash mariadb_repo_setup --mariadb-server-version=11.2

# Install MariaDB
sudo apt update
sudo apt install -y mariadb-server
```

Secure the MariaDB installation:
```bash
sudo mysql_secure_installation
```
Follow the prompts to set a root password and secure the database server.

### 4. Install and Configure Nginx

```bash
sudo apt install -y nginx
```

Enable the firewall and allow Nginx:
```bash
sudo apt install -y ufw
sudo ufw allow 'Nginx Full'
sudo ufw allow ssh
sudo ufw enable
```

## Application Deployment

### 1. Set Environment Variables

Create a deployment environment file:
```bash
sudo nano /etc/chatroom-env
```

Add the following content:
```bash
REPO_PATH=/opt/chatroom
DOMAIN=your-domain.com
MARIADB_PASSWORD=your_secure_password
DB_USER=chatroomuser
DB_NAME=chatroom
```

Load the environment variables:
```bash
source /etc/chatroom-env
```

### 2. Clone the Repository

```bash
sudo mkdir -p $REPO_PATH
sudo chown $(whoami):$(whoami) $REPO_PATH
git clone https://github.com/yourusername/nextjs-dotnet-chatroom.git $REPO_PATH
cd $REPO_PATH
```

### 3. Run the Deployment Script

Make the deployment scripts executable:
```bash
chmod +x scripts/deploy.sh
chmod +x scripts/setup_https.sh
chmod +x scripts/backup_db.sh
```

Run the main deployment script:
```bash
sudo -E ./scripts/deploy.sh
```

This script will:
1. Create the database and user
2. Build and deploy the backend application
3. Build and deploy the frontend application
4. Configure Nginx
5. Set up systemd service for the backend

### 4. Set Up HTTPS

Run the HTTPS setup script:
```bash
sudo -E ./scripts/setup_https.sh
```

This script will:
1. Install Certbot
2. Obtain SSL certificates for your domain
3. Configure Nginx to use HTTPS
4. Set up automatic certificate renewal

## Checking the Deployment

### Verify Services

Check the backend service status:
```bash
sudo systemctl status chatroom-api
```

Check Nginx status:
```bash
sudo systemctl status nginx
```

### Test the Application

Visit your domain in a web browser:
```
https://your-domain.com
```

### View Logs

Backend logs:
```bash
sudo journalctl -u chatroom-api -f
```

Nginx logs:
```bash
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

## Database Backups

Set up automatic daily backups:
```bash
echo "0 2 * * * root MARIADB_PASSWORD=$MARIADB_PASSWORD $REPO_PATH/scripts/backup_db.sh" | sudo tee /etc/cron.d/chatroom-backups
sudo chmod 0644 /etc/cron.d/chatroom-backups
```

This will create daily backups at 2 AM and store them in `/var/backups/chatroom/`.

## Scaling and Load Balancing

For higher traffic loads, consider the following:

1. **Database Scaling**:
   - Set up MariaDB replication with a read replica
   - Use connection pooling

2. **Backend Scaling**:
   - Deploy multiple instances behind a load balancer
   - Use Redis for SignalR backplane to coordinate between instances

3. **Frontend Scaling**:
   - Deploy to a CDN for static assets
   - Use a load balancer for multiple frontend instances

## Troubleshooting

### Backend Fails to Start

Check the logs:
```bash
sudo journalctl -u chatroom-api -f
```

Common issues:
- Database connection problems: Verify the connection string in `appsettings.Production.json`
- Permission issues: Ensure the application directory has the correct permissions
- Port conflicts: Check if port 5000 is already in use

### Nginx Configuration Issues

Test the Nginx configuration:
```bash
sudo nginx -t
```

If there are syntax errors, fix them in the configuration files in `/etc/nginx/sites-available/`.

### SSL Certificate Issues

Check Certbot logs:
```bash
sudo certbot certificates
```

If certificates are missing or expired, renew them:
```bash
sudo certbot renew --force-renewal
```

## Maintenance

### Updating the Application

Pull the latest changes:
```bash
cd $REPO_PATH
git pull
```

Run the deployment script again:
```bash
sudo -E ./scripts/deploy.sh
```

### Database Maintenance

Perform regular database maintenance:
```bash
sudo mysql -u root -p
```

```sql
-- Optimize tables
OPTIMIZE TABLE chatroom.Messages;

-- Check for and repair any corrupted tables
CHECK TABLE chatroom.Messages;
REPAIR TABLE chatroom.Messages;
```

### System Updates

Keep the system updated:
```bash
sudo apt update
sudo apt upgrade -y
```

Restart services after significant updates:
```bash
sudo systemctl restart chatroom-api
sudo systemctl restart nginx
```
