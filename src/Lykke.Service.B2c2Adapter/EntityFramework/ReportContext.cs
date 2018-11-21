using Lykke.Service.B2c2Adapter.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace Lykke.Service.B2c2Adapter.EntityFramework
{
    public class ReportContext : DbContext
    {
        private readonly string _connectionString;

        public ReportContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbSet<TradeEntity> Trades { get; set; }

        public DbSet<BalanceEntity> Balances { get; set; }

        public DbSet<LedgerEntity> Ledgers { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<BalanceEntity>().HasKey(e => new {
                e.Asset,
                e.Timestamp
            });
        }
    }
}
