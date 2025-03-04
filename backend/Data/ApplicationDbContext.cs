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
        }
    }
}
