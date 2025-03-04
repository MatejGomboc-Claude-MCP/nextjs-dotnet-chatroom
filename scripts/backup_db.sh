#!/bin/bash

# Database backup script for Chatroom application
# This script should be run as a cron job for regular backups

# Configuration
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_DIR="/var/backups/chatroom"
RETENTION_DAYS=7

# Check if environment variables are set, use defaults if not
DB_NAME=${DB_NAME:-chatroom}
DB_USER=${DB_USER:-chatroomuser}

# Ensure the backup directory exists
mkdir -p $BACKUP_DIR

# Create backup filename
BACKUP_FILE="$BACKUP_DIR/$DB_NAME-$TIMESTAMP.sql.gz"

# Export database to backup file
echo "Creating backup of $DB_NAME database..."
mysqldump -u $DB_USER -p$MARIADB_PASSWORD $DB_NAME | gzip > $BACKUP_FILE

# Check if backup was successful
if [ $? -eq 0 ]; then
  echo "Backup completed successfully: $BACKUP_FILE"
  
  # Set proper permissions
  chmod 600 $BACKUP_FILE
  
  # Remove backups older than retention period
  echo "Removing backups older than $RETENTION_DAYS days..."
  find $BACKUP_DIR -name "$DB_NAME-*.sql.gz" -mtime +$RETENTION_DAYS -delete
else
  echo "Error: Backup failed!"
  exit 1
fi

# Output summary
echo "Backup Summary:"
echo "---------------"
echo "Database: $DB_NAME"
echo "Backup File: $BACKUP_FILE"
echo "Backup Size: $(du -h $BACKUP_FILE | cut -f1)"
echo "Retention Policy: $RETENTION_DAYS days"
echo "Available Backups:"
ls -lh $BACKUP_DIR | grep "$DB_NAME-" | tail -n 5

echo "To restore this backup, use:"
echo "zcat $BACKUP_FILE | mysql -u $DB_USER -p $DB_NAME"

exit 0
