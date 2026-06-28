using Microsoft.EntityFrameworkCore;
using ZerodaTrade.Models;

namespace ZerodaTrade.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Watchlist> Watchlists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Watchlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ScriptName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.GroupName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Price).HasPrecision(10, 2);
            });
        }
    }
}
