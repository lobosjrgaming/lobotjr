using LobotJR.Data;
using Microsoft.EntityFrameworkCore;

namespace LobotJR.Test.Mocks
{
    /// <summary>
    /// Adds data to the in-memory database during initialization.
    /// </summary>
    /// <param name="context"></param>
    public delegate void ContextInitializer(MockContext context);

    /// <summary>
    /// In-memory sqlite database used as the database connection during unit tests.
    /// </summary>
    public class MockContext : SqliteContext
    {
        public static MockContext Create()
        {
            return new MockContext();
        }

        public MockContext()
        {
            var deleted = Database.EnsureDeleted();
            var created = Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlite("DataSource=:memory:");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
