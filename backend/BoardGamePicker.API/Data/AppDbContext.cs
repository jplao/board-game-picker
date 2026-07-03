using BoardGamePicker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BoardGamePicker.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<BoardGame> BoardGames => Set<BoardGame>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoardGame>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).IsRequired().HasMaxLength(200);
            e.Property(g => g.Type).HasMaxLength(100);
            e.Property(g => g.Category).HasMaxLength(100);
        });
    }
}
