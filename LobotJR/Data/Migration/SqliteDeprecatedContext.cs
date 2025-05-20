using Microsoft.EntityFrameworkCore;

namespace LobotJR.Data.Migration
{
    /// <summary>
    /// SQLite implementation of the EF6 DbContext
    /// </summary>
    public class SqliteDeprecatedContext : DbContext
    {
        public DbSet<DeprecatedAppSettings> AppSettings { get; set; }

        public SqliteDeprecatedContext()
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
