using Microsoft.EntityFrameworkCore;

namespace LobotJR.Data.Migration
{
    /// <summary>
    /// SQLite implementation of the EF6 DbContext
    /// </summary>
    public class SqliteUpdateContext : DbContext
    {
        public DbSet<Metadata> Metadata { get; set; }

        public SqliteUpdateContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data source=.\\data.sqlite");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
