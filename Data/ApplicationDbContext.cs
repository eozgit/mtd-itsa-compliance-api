
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Initialize DbSets with 'null!' to silence CS8618 warnings
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Business> Businesses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id); // Specify Id as primary key
            entity.HasIndex(u => u.Email).IsUnique(); // Ensure Email is unique
        });

        // Configure Business entity
        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasOne(b => b.User) // A Business has one User
                  .WithMany() // A User can have many Businesses (or configure WithOne if 1-to-1)
                  .HasForeignKey(b => b.UserId) // UserId is the foreign key
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade); // If User is deleted, delete Businesses
        });
    }
}
