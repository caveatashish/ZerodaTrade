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

        public DbSet<Group> Groups { get; set; }
        public DbSet<Script> Scripts { get; set; }
        public DbSet<Trade> Trades { get; set; }

        public DbSet<DailyTrade> DailyTrades{ get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          

            modelBuilder.Entity<DailyTrade>(entity =>
            {
                entity.HasKey(e => e.TradeId);
                entity.Property(e => e.FillTime).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Instrument).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CNC);
                entity.Property(e => e.Qty).IsRequired();
                entity.Property(e => e.AvgPrice).HasPrecision(18, 2);
                entity.Property(e => e.CreatedDate);
                entity.Property(e => e.ModifiedDate);
            });
        }

      
    }
}
