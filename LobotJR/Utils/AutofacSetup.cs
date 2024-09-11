using Autofac;
using Autofac.Core;
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
using LobotJR.Data.Migration;
using LobotJR.Shared.Authentication;
using LobotJR.Shared.Client;
using LobotJR.Trigger;
using LobotJR.Trigger.Responder;
using LobotJR.Twitch;

namespace LobotJR.Utils
{
    public static class AutofacSetup
    {
        public static IContainer SetupUpdater(ClientData clientData, TokenData tokenData)
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<DatabaseUpdate_Null_1_0_0>().As<IDatabaseUpdate>().InstancePerLifetimeScope()
                .WithParameters(new Parameter[] { new TypedParameter(typeof(ClientData), clientData), new TypedParameter(typeof(TokenData), tokenData) });
            builder.RegisterType<DatabaseUpdate_1_0_0_1_0_1>().As<IDatabaseUpdate>().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseUpdate_1_0_1_1_0_2>().As<IDatabaseUpdate>().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseUpdate_1_0_2_1_0_3>().As<IDatabaseUpdate>().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseUpdate_1_0_3_1_0_4>().As<IDatabaseUpdate>().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseUpdate_1_0_4_1_0_5>().As<IDatabaseUpdate>().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseUpdate_1_0_5_1_0_6>().As<IDatabaseUpdate>().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseUpdate_1_0_6_1_0_7>().As<IDatabaseUpdate>().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseUpdate_1_0_7_1_1_0>().As<IDatabaseUpdate>().InstancePerLifetimeScope();

            builder.RegisterType<SqliteDatabaseUpdater>().AsSelf().InstancePerLifetimeScope();

            return builder.Build();
        }

        private static void RegisterDatabase(ContainerBuilder builder)
        {
            // builder.RegisterType<SqliteContext>().AsSelf().As<DbContext>().InstancePerLifetimeScope();
            // builder.RegisterType<SqliteRepositoryManager>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            // These are created by the connection manager now, I think
            builder.RegisterType<ConnectionManager>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
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
            builder.RegisterType<UserAdmin>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AccessControlView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AccessControlAdmin>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<InfoView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ConfirmationView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            // builder.RegisterType<BettingView>().AsSelf().As<ICommandView>().InstancePerLifetimeScope();

            builder.RegisterType<EquipmentView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<EquipmentAdmin>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PetView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PetAdmin>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<PlayerView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<PlayerAdmin>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<FishingAdmin>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<FishingView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<LeaderboardView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TournamentView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<DungeonView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<GroupFinderView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<GroupFinderAdmin>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<GloatView>().AsSelf().AsImplementedInterfaces().InstancePerLifetimeScope();
        }

        private static void RegisterTriggers(ContainerBuilder builder)
        {
            builder.RegisterType<BlockLinks>().AsSelf().As<ITriggerResponder>().InstancePerLifetimeScope();
            builder.RegisterType<NoceanMan>().AsSelf().As<ITriggerResponder>().InstancePerLifetimeScope();
            builder.RegisterType<BadLobot>().AsSelf().As<ITriggerResponder>().InstancePerLifetimeScope();
        }

        private static void RegisterManagers(ContainerBuilder builder, ClientData clientData, TokenData tokenData)
        {
            builder.RegisterType<TwitchClient>().AsSelf().As<ITwitchClient>().InstancePerLifetimeScope()
                .WithParameters(new Parameter[] { new TypedParameter(typeof(ClientData), clientData), new TypedParameter(typeof(TokenData), tokenData) });
            builder.RegisterType<TwitchIrcClient>().AsSelf().As<ITwitchIrcClient>().InstancePerLifetimeScope()
                .WithParameters(new Parameter[] { new TypedParameter(typeof(TokenData), tokenData) });
            builder.RegisterType<ControllerManager>().AsSelf().As<IControllerManager>().InstancePerLifetimeScope();
            builder.RegisterType<CommandManager>().AsSelf().As<ICommandManager>().InstancePerLifetimeScope();
            builder.RegisterType<TriggerManager>().AsSelf().InstancePerLifetimeScope();
        }

        public static IContainer Setup(ClientData clientData, TokenData tokenData)
        {
            var builder = new ContainerBuilder();

            RegisterDatabase(builder);
            RegisterControllers(builder);
            RegisterViews(builder);
            RegisterTriggers(builder);
            RegisterManagers(builder, clientData, tokenData);

            return builder.Build();
        }
    }
}
