using LobotJR.Command.Module;
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
        public CommandExecutor(ICommandModule target, MethodInfo methodInfo) : base(target, methodInfo)
        {
            if (methodInfo.ReturnType != typeof(CommandResult))
            {
                throw new Exception($"Delegate for command executor must have return type of {typeof(CommandResult)}");
            }
        }
    }

    public class CompactExecutor : GenericExecutor<ICompactResponse>
    {
        public CompactExecutor(ICommandModule target, MethodInfo methodInfo) : base(target, methodInfo)
        {
            if (methodInfo.ReturnType != typeof(ICompactResponse) && methodInfo.ReturnType.GetInterface(nameof(ICompactResponse)) == null)
            {
                throw new Exception($"Delegate for compact executor must have return type of {typeof(ICompactResponse)}");
            }
        }
    }

    public class GenericExecutor<T> where T : class
    {
        private object Target;
        private MethodInfo MethodInfo;
        private ParameterInfo[] Parameters;
        private int MinParams;
        private int MaxParams;
        private bool HasUserParam;

        public GenericExecutor(object target, MethodInfo methodInfo)
        {
            Target = target;
            MethodInfo = methodInfo;
            Parameters = MethodInfo.GetParameters();
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
            return type.Name.Substring(type.Name.LastIndexOf('.') + 1);
        }

        public string DescribeParameters()
        {
            return string.Join(", ", MethodInfo.GetParameters().Where(x => x.ParameterType != typeof(User)).Select(x => $"{(x.HasDefaultValue ? "[optional]" : "")}{x.ParameterType.Name} {x.Name}"));
        }

        public T Execute(User user, string parameterString)
        {
            if (string.IsNullOrWhiteSpace(parameterString))
            {
                parameterString = string.Empty;
            }
            var passed = SplitParams(parameterString).ToArray();
            if (passed.Length < MinParams || passed.Length > MaxParams)
            {
                throw new ArgumentException($"Invalid parameters. Syntax: {DescribeParameters()}.");
            }

            string typeExceptions = "";
            object[] toPass = new object[Parameters.Length];
            var userAdjust = 0;
            if (HasUserParam)
            {
                userAdjust = 1;
                toPass[0] = user;
            }

            for (var i = 0; i < passed.Length; i++)
            {
                var param = passed[i];
                var targetParam = Parameters[i + userAdjust];
                if (TryChangeType(param, targetParam.ParameterType, out var result))
                {
                    toPass[i + userAdjust] = result;
                }
                else
                {
                    typeExceptions += $"Can't convert {param} to {SimplifyType(targetParam.ParameterType)}.";
                }
            }
            for (var i = passed.Length + userAdjust; i < MaxParams + userAdjust; i++)
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
