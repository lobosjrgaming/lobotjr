using LobotJR.Command.Model.AccessControl;
using SQLite.CodeFirst;
using System.Data.Entity;

namespace LobotJR.Data
{
    public class SqliteInitializer : SqliteCreateDatabaseIfNotExists<SqliteContext>
    {
        public SqliteInitializer(DbModelBuilder dbModelBuilder) : base(dbModelBuilder) { }
        protected override void Seed(SqliteContext context)
        {
            context.AccessGroups.Add(new AccessGroup() { Id = 1, Name = "Admin", IncludeAdmins = true });
            context.AccessGroups.Add(new AccessGroup() { Id = 2, Name = "UIDev" });
            context.Enrollments.Add(new Enrollment() { GroupId = 2, UserId = "26374083" });
            if (context.Metadata != null)
            {
                context.Metadata.Add(new Metadata());
            }
            context.AppSettings.Add(new AppSettings());
        }
    }
}
