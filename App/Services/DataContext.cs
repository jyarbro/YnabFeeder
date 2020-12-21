using Microsoft.EntityFrameworkCore;
using Nrrdio.Utilities.Loggers;

namespace App {
    public class DataContext : DbContext {
        public DbSet<LogEntry> LogEntries { get; set; }

        public DataContext(DbContextOptions<DataContext> options) : base(options) { 
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlite("Data Source=AppData.db");
        }
    }
}
