using LobotJR.Command;
using LobotJR.Twitch.Model;
using System;
using System.Reflection;

namespace LobotJR.Utils
{
    /// <summary>
    /// This is a static class that is used to get the MethodInfo object from a method when building command handlers.
    /// </summary>
    public static class CommandMethod
    {
        public static MethodInfo GetInfo(Func<CommandResult> func) { return func.Method; }
        public static MethodInfo GetInfo(Func<User, CommandResult> func) { return func.Method; }

        public static MethodInfo GetInfo<T>(Func<User, T, CommandResult> func) { return func.Method; }
        public static MethodInfo GetInfo<T1, T2>(Func<User, T1, T2, CommandResult> func) { return func.Method; }
        public static MethodInfo GetInfo<T1, T2, T3>(Func<User, T1, T2, T3, CommandResult> func) { return func.Method; }

        public static MethodInfo GetInfo<T>(Func<T, CommandResult> func) { return func.Method; }
        public static MethodInfo GetInfo<T1, T2>(Func<T1, T2, CommandResult> func) { return func.Method; }
        public static MethodInfo GetInfo<T1, T2, T3>(Func<T1, T2, T3, CommandResult> func) { return func.Method; }

        public static MethodInfo GetInfo(Func<ICompactResponse> func) { return func.Method; }
        public static MethodInfo GetInfo(Func<User, ICompactResponse> func) { return func.Method; }

        public static MethodInfo GetInfo<T>(Func<User, T, ICompactResponse> func) { return func.Method; }

        public static MethodInfo GetInfo<T>(Func<T, ICompactResponse> func) { return func.Method; }
    }
}
