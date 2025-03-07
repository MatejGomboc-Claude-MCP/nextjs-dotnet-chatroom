using ChatRoom.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatRoom.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<MessageReaction> MessageReactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Message entity
            modelBuilder.Entity<Message>()
                .HasKey(m => m.Id);
                
            modelBuilder.Entity<Message>()
                .Property(m => m.Text)
                .IsRequired();
                
            modelBuilder.Entity<Message>()
                .Property(m => m.Username)
                .IsRequired();
                
            modelBuilder.Entity<Message>()
                .Property(m => m.Timestamp)
                .IsRequired();
                
            // Configure MessageReaction entity
            modelBuilder.Entity<MessageReaction>()
                .HasKey(r => r.Id);
                
            modelBuilder.Entity<MessageReaction>()
                .Property(r => r.Emoji)
                .IsRequired()
                .HasMaxLength(10);
                
            modelBuilder.Entity<MessageReaction>()
                .Property(r => r.Username)
                .IsRequired()
                .HasMaxLength(50);
                
            // Configure relationship
            modelBuilder.Entity<MessageReaction>()
                .HasOne(r => r.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Add unique constraint to prevent duplicate reactions from the same user
            modelBuilder.Entity<MessageReaction>()
                .HasIndex(r => new { r.MessageId, r.Username, r.Emoji })
                .IsUnique();
        }
    }
}
