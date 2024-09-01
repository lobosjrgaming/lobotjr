using Autofac;
using LobotJR.Command;
using LobotJR.Command.Controller;
using LobotJR.Command.Controller.AccessControl;
using LobotJR.Command.Controller.Dungeons;
using LobotJR.Command.Controller.Equipment;
using LobotJR.Command.Controller.Fishing;
using LobotJR.Command.Controller.General;
using LobotJR.Command.Controller.Gloat;
using LobotJR.Command.Controller.Pets;
using LobotJR.Command.Controller.Player;
using LobotJR.Command.Controller.Twitch;
using LobotJR.Command.View;
using LobotJR.Command.View.AccessControl;
using LobotJR.Command.View.Dungeons;
using LobotJR.Command.View.Equipment;
using LobotJR.Command.View.Fishing;
using LobotJR.Command.View.General;
using LobotJR.Command.View.Gloat;
using LobotJR.Command.View.Pets;
using LobotJR.Command.View.Player;
using LobotJR.Command.View.Twitch;
using LobotJR.Data;
using LobotJR.Trigger;
using LobotJR.Trigger.Responder;
using LobotJR.Twitch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LobotJR.Test.Mocks
{
    [TestClass]
    public static class AutofacMockSetup
    {
        public static IContainer Container { get; private set; }
        public static MockConnectionManager ConnectionManager { get; private set; }

        private static void RegisterDatabase(ContainerBuilder builder)
        {
            // builder.RegisterType<SqliteContext>().AsSelf().As<DbContext>().InstancePerLifetimeScope();
            // builder.RegisterType<SqliteRepositoryManager>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            // These are created by the connection manager now, I think
            builder.RegisterType<MockConnectionManager>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SettingsManager>().AsSelf().InstancePerLifetimeScope();
        }

        private static void RegisterControllers(ContainerBuilder builder)
        {
            builder.RegisterType<UserController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AccessControlController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<BugReportController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ConfirmationController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            // builder.RegisterType<BettingController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<PlayerController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<EquipmentController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PetController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<DungeonController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<GroupFinderController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PartyController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<FishingController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<LeaderboardController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TournamentController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<GloatController>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
        }

        private static void RegisterViews(ContainerBuilder builder)
        {
            builder.RegisterType<UserAdmin>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<AccessControlView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<AccessControlAdmin>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();

            builder.RegisterType<InfoView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<ConfirmationView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            // builder.RegisterType<BettingView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();

            builder.RegisterType<EquipmentView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<EquipmentAdmin>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<PetView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<PetAdmin>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();

            builder.RegisterType<PlayerView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<PlayerAdmin>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();

            builder.RegisterType<FishingAdmin>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<FishingView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<LeaderboardView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<TournamentView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();

            builder.RegisterType<DungeonView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<GroupFinderView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<GroupFinderAdmin>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();

            builder.RegisterType<GloatView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();

            builder.RegisterType<MockCommandView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
            builder.RegisterType<MockCommandSubView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();
        }

        private static void RegisterTriggers(ContainerBuilder builder)
        {
            builder.RegisterType<BlockLinks>().AsSelf().As<ITriggerResponder>().InstancePerLifetimeScope();
            builder.RegisterType<NoceanMan>().AsSelf().As<ITriggerResponder>().InstancePerLifetimeScope();
            builder.RegisterType<BadLobot>().AsSelf().As<ITriggerResponder>().InstancePerLifetimeScope();
        }

        private static void RegisterManagers(ContainerBuilder builder)
        {
            builder.RegisterType<MockTwitchClient>().AsSelf().As<ITwitchClient>().InstancePerLifetimeScope();
            builder.RegisterInstance(new Mock<ITwitchIrcClient>().Object).SingleInstance();
            builder.RegisterType<ControllerManager>().AsSelf().As<IControllerManager>().InstancePerLifetimeScope();
            builder.RegisterType<CommandManager>().AsSelf().As<ICommandManager>().InstancePerLifetimeScope();
            builder.RegisterType<TriggerManager>().AsSelf().InstancePerLifetimeScope();
        }

        public static void ResetAccessGroups()
        {
            ConnectionManager.ResetAccessGroups();
        }

        public static void ResetFishingRecords()
        {
            ConnectionManager.ResetFishingData();
        }

        public static void ResetUsers()
        {
            ConnectionManager.ResetUsers();
        }

        public static void ResetPlayers()
        {
            ConnectionManager.ResetPlayers();
        }

        [AssemblyInitialize]
        public static void Setup(TestContext _)
        {
            var builder = new ContainerBuilder();

            RegisterDatabase(builder);
            RegisterControllers(builder);
            RegisterViews(builder);
            RegisterTriggers(builder);
            RegisterManagers(builder);

            var container = builder.Build();
            Container = container;
            ConnectionManager = container.Resolve<MockConnectionManager>();
            var commandManager = container.Resolve<ICommandManager>();
            var controllerManager = container.Resolve<IControllerManager>();
            ConnectionManager.OpenConnection();
            ConnectionManager.SeedData();
            controllerManager.Initialize();

            commandManager.InitializeViews();
            ConnectionManager.CurrentConnection.Commit();
        }
    }
}
