using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NeoSharp.Application.Attributes;
using NeoSharp.Application.Client;
using NeoSharp.Core.DI;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Types;

namespace NeoSharp.Application.Extensions
{
    public static class CacheExtensions
    {
        public static void Cache(this IDictionary<string[], PromptCommandAttribute> cache, Type type, out IAutoCompleteHandler autoComplete)
        {
            var commandAutocompleteCache = new Dictionary<string, List<ParameterInfo[]>>();

            foreach (var mi in type.GetMethods
                (
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
                ))
            {
                var atr = mi.GetCustomAttribute<PromptCommandAttribute>();

                if (atr == null) continue;

                atr.SetMethod(mi);

                cache.Add(atr.Commands, atr);

                if (commandAutocompleteCache.ContainsKey(atr.Command))
                {
                    commandAutocompleteCache[atr.Command].Add(mi.GetParameters());
                }
                else
                {
                    var ls = new List<ParameterInfo[]>
                    {
                        mi.GetParameters()
                    };

                    commandAutocompleteCache.Add(atr.Command, ls);
                }
            }

            autoComplete = new AutoCommandComplete(commandAutocompleteCache);
        }

        public static T SearchRightCommand<T>(this T[] cmds, IEnumerable<CommandToken> args, IContainer injector) where T : PromptCommandAttribute
        {
            switch (cmds.Length)
            {
                case 0: return default(T);
                case 1: return cmds[0];
                default:
                    {
                        // Multiple commands

                        var cmd = default(T);

                        foreach (var a in cmds)
                        {
                            try
                            {
                                a.ConvertToArguments(args.Skip(a.TokensCount).ToArray(), injector);

                                if (cmd == null || cmd.Order > a.Order)
                                    cmd = a;
                            }
                            catch { }
                        }

                        return cmd;
                    }
            }
        }

        public static IEnumerable<T> SearchCommands<T>(this IDictionary<string[], T> cache, IList<CommandToken> cmdArgs)
        {
            // Parse arguments

            if (cmdArgs.Count <= 0) yield break;

            foreach (var key in cache)
            {
                if (key.Key.Length > cmdArgs.Count) continue;

                var equal = true;
                for (int x = 0, m = key.Key.Length; x < m; x++)
                {
                    var c = cmdArgs[x];
                    if (c.Value.ToLowerInvariant() != key.Key[x])
                    {
                        equal = false;
                        break;
                    }
                }

                if (equal)
                {
                    yield return key.Value;
                }
            }
        }
    }
}