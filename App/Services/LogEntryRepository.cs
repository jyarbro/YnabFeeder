using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.Loggers.Contracts;

namespace App.Services {
    class LogEntryRepository : ILogEntryRepository {
        DataContext Db { get; init; }

        public LogEntryRepository(
            DataContext db
        ) {
            Db = db;
        }

        public void Add(LogEntry logEntry) {
            Db.Add(logEntry);
            Db.SaveChanges();
        }
    }
}
