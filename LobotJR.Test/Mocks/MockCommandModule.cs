using LobotJR.Command;
using LobotJR.Command.Module;
using LobotJR.Twitch.Model;
using LobotJR.Utils;
using System.Collections.Generic;

namespace LobotJR.Test.Mocks
{
    /// <summary>
    /// Mock Command Module used for testing command manager functionality.
    /// </summary>
    public class MockCommandModule : ICommandModule
    {
        public string Name => "CommandMock";
        public IEnumerable<CommandHandler> Commands { get; private set; }
        public event PushNotificationHandler PushNotification;

        public int FooCount { get; private set; } = 0;
        public int FooCountCompact { get; private set; } = 0;
        public int PublicCount { get; private set; } = 0;
        public int ModFooCount { get; private set; } = 0;
        public int SubFooCount { get; private set; } = 0;
        public int VipFooCount { get; private set; } = 0;
        public int AdminFooCount { get; private set; } = 0;
        public int SingleParamCount { get; private set; } = 0;
        public int MultiParamCount { get; private set; } = 0;
        public int IntParamCount { get; private set; } = 0;
        public int BoolParamCount { get; private set; } = 0;
        public int OptionalParamCount { get; private set; } = 0;
        public int UserParamCount { get; private set; } = 0;
        public int UserAndStringParamCount { get; private set; } = 0;
        public int TotalCount { get { return FooCount + FooCountCompact + PublicCount + ModFooCount + SubFooCount + VipFooCount + AdminFooCount + SingleParamCount + MultiParamCount + IntParamCount + BoolParamCount + OptionalParamCount + UserParamCount + UserAndStringParamCount; } }

        public MockCommandModule()
        {
            Commands = new CommandHandler[]
            {
                new CommandHandler("Foo", this, CommandMethod.GetInfo(Foo), CommandMethod.GetInfo<string>(FooCompact), "Foo"),
                new CommandHandler("Unrestricted", this, CommandMethod.GetInfo(Foo), CommandMethod.GetInfo<string>(FooCompact), "Unrestricted"),
                new CommandHandler("Public", this, CommandMethod.GetInfo(Public), "Public") { WhisperOnly = false },
                new CommandHandler("ModFoo", this, CommandMethod.GetInfo(ModFoo), "ModFoo"),
                new CommandHandler("SubFoo", this, CommandMethod.GetInfo(SubFoo), "SubFoo"),
                new CommandHandler("VipFoo", this, CommandMethod.GetInfo(VipFoo), "VipFoo"),
                new CommandHandler("AdminFoo", this, CommandMethod.GetInfo(AdminFoo), "AdminFoo"),
                new CommandHandler("SingleParam", this, CommandMethod.GetInfo<string>(SingleParam), "SingleParam"),
                new CommandHandler("MultiParam", this, CommandMethod.GetInfo<string, string>(MultiParam), "MultiParam"),
                new CommandHandler("IntParam", this, CommandMethod.GetInfo<int>(IntParam), "IntParam"),
                new CommandHandler("BoolParam", this, CommandMethod.GetInfo<bool>(BoolParam), "BoolParam"),
                new CommandHandler("OptionalParam", this, CommandMethod.GetInfo<string>(OptionalParam), "OptionalParam"),
                new CommandHandler("UserParam", this, CommandMethod.GetInfo(UserParam), "UserParam"),
                new CommandHandler("UserAndStringParam", this, CommandMethod.GetInfo<string>(UserAndStringParam), "UserAndStringParam"),
            };
        }

        public CommandResult Foo()
        {
            FooCount++;
            return new CommandResult(true);
        }

        public CommandResult Public()
        {
            PublicCount++;
            return new CommandResult(true);
        }

        public CommandResult ModFoo()
        {
            ModFooCount++;
            return new CommandResult(true);
        }

        public CommandResult SubFoo()
        {
            SubFooCount++;
            return new CommandResult(true);
        }

        public CommandResult VipFoo()
        {
            VipFooCount++;
            return new CommandResult(true);
        }

        public CommandResult AdminFoo()
        {
            AdminFooCount++;
            return new CommandResult(true);
        }

        public CommandResult SingleParam(string p1)
        {
            SingleParamCount++;
            return new CommandResult($"Received parameter {p1}");
        }

        public CommandResult MultiParam(string p1, string p2)
        {
            MultiParamCount++;
            return new CommandResult($"Received parameters {p1}, {p2}");
        }

        public CommandResult IntParam(int p1)
        {
            IntParamCount++;
            return new CommandResult($"Received parameter {p1}");
        }

        public CommandResult BoolParam(bool p1)
        {
            BoolParamCount++;
            return new CommandResult($"Received parameter {p1}");
        }

        public CommandResult UserParam(User user)
        {
            UserParamCount++;
            return new CommandResult($"Received parameter {user.Username}");
        }

        public CommandResult UserAndStringParam(User user, string p1)
        {
            UserAndStringParamCount++;
            return new CommandResult($"Received parameter {user.Username}, {p1}");
        }

        public CommandResult OptionalParam(string p1 = "default")
        {
            OptionalParamCount++;
            return new CommandResult($"Received parameter {p1}");
        }

        public ICompactResponse FooCompact(string data = "Bar")
        {
            FooCountCompact++;
            return new CompactCollection<string>(new string[] { data }, x => $"Foo|{x};");
        }
    }

    public class MockCommandSubModule : ICommandModule
    {
        public string Name => "CommandMock.SubMock";
        public IEnumerable<CommandHandler> Commands { get; private set; }
        public event PushNotificationHandler PushNotification;

        public int FoobarCount { get; private set; } = 0;
        public MockCommandSubModule()
        {
            Commands = new CommandHandler[]
            {
                new CommandHandler("Foobar", this, CommandMethod.GetInfo(Foo), "Foobar"),
            };
        }

        public CommandResult Foo()
        {
            FoobarCount++;
            return new CommandResult(true);
        }
    }
}
