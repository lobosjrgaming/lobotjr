﻿using LobotJR.Command.View;
using LobotJR.Twitch.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LobotJR.Command
{
    public class CommandExecutor : GenericExecutor<CommandResult>
    {
        public CommandExecutor(ICommandView target, MethodInfo methodInfo) : base(target, methodInfo, false)
        {
            if (methodInfo.ReturnType != typeof(CommandResult))
            {
                throw new Exception($"Delegate for command executor must have return type of {typeof(CommandResult)}");
            }
        }
        public CommandExecutor(ICommandView target, MethodInfo methodInfo, bool ignoreParse) : base(target, methodInfo, ignoreParse)
        {
            if (methodInfo.ReturnType != typeof(CommandResult))
            {
                throw new Exception($"Delegate for command executor must have return type of {typeof(CommandResult)}");
            }
            if (ignoreParse)
            {
                var parameters = methodInfo.GetParameters();
                if ((parameters.Length == 1 && parameters[0].ParameterType != typeof(string))
                    || (parameters.Length == 2 && parameters[1].ParameterType != typeof(string))
                    || parameters.Length > 2 || parameters.Length < 1)
                {
                    throw new Exception($"Delegate for command executor that ignores parse must have one string parameter to receive unparsed input.");
                }
            }
        }
    }

    public class CompactExecutor : GenericExecutor<ICompactResponse>
    {
        public CompactExecutor(ICommandView target, MethodInfo methodInfo) : base(target, methodInfo, false)
        {
            if (methodInfo.ReturnType != typeof(ICompactResponse) && methodInfo.ReturnType.GetInterface(nameof(ICompactResponse)) == null)
            {
                throw new Exception($"Delegate for compact executor must have return type of {typeof(ICompactResponse)}");
            }
        }
    }

    public class GenericExecutor<T> where T : class
    {
        private readonly object Target;
        private readonly MethodInfo MethodInfo;
        private readonly ParameterInfo[] Parameters;
        private readonly int MinParams;
        private readonly int MaxParams;
        private readonly bool HasUserParam;
        private readonly bool SkipParse;

        public GenericExecutor(object target, MethodInfo methodInfo, bool skipParse)
        {
            Target = target;
            MethodInfo = methodInfo;
            Parameters = MethodInfo.GetParameters();
            SkipParse = skipParse;
            MaxParams = Parameters.Length;
            MinParams = MaxParams - Parameters.Count(x => x.HasDefaultValue);
            var userParams = Parameters.Count(x => x.ParameterType == typeof(User));
            if (userParams == 1)
            {
                HasUserParam = true;
                MaxParams--;
                MinParams--;
                if (Parameters[0].ParameterType != typeof(User))
                {
                    throw new Exception($"User parameter must be the first parameter");
                }
            }
            else if (userParams > 1)
            {
                throw new Exception($"Command executor functions can only specify a single User parameter");
            }
        }

        private IEnumerable<string> SplitParams(string parameterString)
        {
            var output = new List<string>();
            var result = new StringBuilder();
            var quoted = false;
            var escaped = false;
            var started = false;
            for (var i = 0; i < parameterString.Length; i++)
            {
                var chr = parameterString[i];
                if (escaped)
                {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                }
                else if (chr == '"')
                {
                    quoted = !quoted;
                    started = true;
                }
                else if (chr == '\\' && i + 1 < parameterString.Length && parameterString[i + 1] == '"')
                {
                    escaped = true;
                }
                else if (chr == ' ' && !quoted)
                {
                    if (started)
                    {
                        output.Add(result.ToString());
                    }
                    result.Clear();
                    started = false;
                }
                else
                {
                    result.Append(chr);
                    started = true;
                }
            }
            if (started)
            {
                output.Add(result.ToString());
            }
            return output;
        }

        private bool TryChangeType(string obj, Type type, out object result)
        {
            result = null;
            try
            {
                if (type == typeof(string))
                {
                    result = obj;
                }
                else if (type == typeof(int) && int.TryParse(obj, out var intParam))
                {
                    result = intParam;
                }
                else if (type == typeof(float) && float.TryParse(obj, out var floatParam))
                {
                    result = floatParam;
                }
                else if (type == typeof(bool) && bool.TryParse(obj, out var boolParam))
                {
                    result = boolParam;
                }
                else
                {
                    result = Convert.ChangeType(obj, type);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string SimplifyType(Type type)
        {
            if (type == typeof(int) || type == typeof(long))
            {
                return "int ";
            }
            if (type == typeof(float) || type == typeof(double))
            {
                return "float ";
            }
            if (type == typeof(bool))
            {
                return "bool ";
            }
            if (type == typeof(string))
            {
                return string.Empty;
            }
            return $"{type.Name.Substring(type.Name.LastIndexOf('.') + 1)} ";
        }

        public string DescribeParameters()
        {
            return string.Join(" ", MethodInfo.GetParameters().Where(x => x.ParameterType != typeof(User)).Select(x => $"{{{(x.HasDefaultValue ? "[optional]" : "")}{SimplifyType(x.ParameterType)}{x.Name}}}"));
        }

        public T Execute(User user, string parameterString)
        {
            if (string.IsNullOrWhiteSpace(parameterString))
            {
                parameterString = string.Empty;
            }

            string typeExceptions = "";
            object[] toPass = new object[Parameters.Length];
            var paramAdjust = 0;
            if (HasUserParam)
            {
                paramAdjust++;
                toPass[0] = user;
            }
            if (SkipParse)
            {
                toPass[paramAdjust] = parameterString;
                return MethodInfo.Invoke(Target, toPass) as T;
            }
            else
            {
                var passed = SplitParams(parameterString).ToArray();
                if (passed.Length < MinParams || passed.Length > MaxParams)
                {
                    throw new ArgumentException($"Invalid parameters. Syntax: {DescribeParameters()}.");
                }

                for (var i = 0; i < passed.Length; i++)
                {
                    var param = passed[i];
                    var targetParam = Parameters[i + paramAdjust];
                    if (TryChangeType(param, targetParam.ParameterType, out var result))
                    {
                        toPass[i + paramAdjust] = result;
                    }
                    else
                    {
                        typeExceptions += $"Can't convert {param} to {SimplifyType(targetParam.ParameterType)}.";
                    }
                }
                for (var i = passed.Length + paramAdjust; i < MaxParams + paramAdjust; i++)
                {
                    toPass[i] = Type.Missing;
                }

                if (!string.IsNullOrWhiteSpace(typeExceptions))
                {
                    throw new InvalidCastException($"Invalid parameters. {string.Join(" ", typeExceptions)}");
                }
                return MethodInfo.Invoke(Target, toPass) as T;
            }
        }
    }
}
