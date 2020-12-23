using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nrrdio.Utilities.Loggers;

namespace App {
    public class DataContext : DbContext {
        public DbSet<LogEntry> LogEntries { get; set; }
        readonly string connectionString;

        public DataContext(IConfiguration configuration) {
            connectionString = configuration.GetConnectionString("DefaultConnection");
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}
