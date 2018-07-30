﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoSharp.Application.Attributes;
using NeoSharp.Application.Extensions;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Network;
using NeoSharp.Core.Types;

namespace NeoSharp.Application.Client
{
    public partial class Prompt : IPrompt
    {
        [Flags]
        public enum LogVerbose : byte
        {
            Off = 0,

            Trace = 1,
            Debug = 2,
            Information = 4,
            Warning = 8,
            Error = 16,
            Critical = 32,

            All = Trace | Debug | Information | Warning | Error | Critical
        }

        private readonly Dictionary<LogLevel, LogVerbose> _logFlagProxy = new Dictionary<LogLevel, LogVerbose>()
        {
            { LogLevel.Trace, LogVerbose.Trace},
            { LogLevel.Debug, LogVerbose.Debug},
            { LogLevel.Information, LogVerbose.Information},
            { LogLevel.Warning, LogVerbose.Warning},
            { LogLevel.Error, LogVerbose.Error},
            { LogLevel.Critical, LogVerbose.Critical},
        };
        private LogVerbose _logVerbose = LogVerbose.Off;

        private void Log_OnLog(LogEntry log)
        {
            if (!_logVerbose.HasFlag(_logFlagProxy[log.Level]))
            {
                return;
            }

            _logs.Add(log);
        }

        private StreamWriter _record;

        /// <summary>
        /// Print help from Multiple commands
        /// </summary>
        /// <param name="cmds">Commands</param>
        void PrintHelp(IEnumerable<PromptCommandAttribute> cmds)
        {
            // Print help

            var cmd = cmds.FirstOrDefault();

            if (cmd != null && !string.IsNullOrEmpty(cmd.Help))
            {
                _consoleWriter.WriteLine(cmd.Help, ConsoleOutputStyle.Information);

                _consoleWriter.WriteLine("");
                _consoleWriter.WriteLine("Examples:", ConsoleOutputStyle.Information);

                // How to use?

                var modes = new List<string>();
                foreach (var v in cmds)
                {
                    var args = "";

                    if (v.Parameters != null && v.Parameters.Length > 0)
                    {
                        foreach (var par in v.Parameters)
                        {
                            if (par.GetCustomAttribute<PromptHideHelpCommandAttribute>() != null) continue;

                            var allowed = "";

                            if (par.ParameterType.IsEnum)
                            {
                                foreach (var o in Enum.GetValues(par.ParameterType))
                                    allowed += (allowed != "" ? "," : "") + o.ToString();

                                allowed = $" {par.Name}={allowed}";

                                if (!modes.Contains(allowed)) modes.Add(allowed);
                            }

                            if (par.HasDefaultValue)
                                args += $" [{par.Name}={(par.DefaultValue == null ? "NULL" : par.DefaultValue.ToString())}]";
                            else
                                args += $" {par.Name}";
                        }
                    }

                    _consoleWriter.WriteLine("  " + v.Command + args, ConsoleOutputStyle.Information);
                }

                if (modes.Count > 0)
                {
                    // Options

                    _consoleWriter.WriteLine("Options:", ConsoleOutputStyle.Information);

                    foreach (var par in modes)
                    {
                        _consoleWriter.WriteLine("  " + par, ConsoleOutputStyle.Information);
                    }
                }
            }
        }

        /// <summary>
        /// Enable / Disable logs
        /// </summary>
        /// <param name="log">Log factory</param>
        /// <param name="mode">Mode</param>
        [PromptCommand("log", Help = "Enable/Disable log output", Category = "Usability")]
        private void LogCommand([PromptHideHelpCommand] ILoggerFactoryExtended log, LogVerbose mode)
        {
            _logVerbose = mode;

            if (mode != LogVerbose.Off)
            {
                log.OnLog -= Log_OnLog;
                log.OnLog += Log_OnLog;

                _logger.LogDebug("Log output is enabled");
            }
            else
            {
                _logs.Clear();
                _logger.LogDebug("Log output is disabled");

                log.OnLog -= Log_OnLog;
            }
        }

        /// <summary>
        /// Watch
        /// </summary>
        /// <param name="ms">Miliseconds</param>
        /// <param name="line">Line</param>
        [PromptCommand("watch", Help = "Watch command", Category = "Usability")]
        private void WatchCommand(uint ms, [PromptCommandParameterBody] string line)
        {
            if (line.Trim().ToLowerInvariant().StartsWith("watch"))
            {
                throw new InvalidOperationException();
            }

            using (var cancel = new CancellationTokenSource())
            {
                // Cancellation listener (wait any key)

                new Task(async () =>
                  {
                      while (!cancel.IsCancellationRequested)
                      {
                          if (_consoleReader.KeyAvailable)
                          {
                              cancel.Cancel();
                              return;
                          }
                          await Task.Delay(10, cancel.Token);
                      }
                  }).Start();

                // watch logic

                while (!cancel.IsCancellationRequested)
                {
                    _consoleWriter.Clear();

                    if (!Execute(line))
                    {
                        break;
                    }

                    try
                    {
                        var ret = Task.Delay((int)ms, cancel.Token);
                        ret.Wait();
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Start current recording
        /// </summary>
        /// <param name="outputFile">File</param>
        [PromptCommand("record start", Help = "Record all commands in a file", Category = "Usability")]
        private void RecordStartCommand(FileInfo outputFile)
        {
            if (_record != null) throw (new Exception("Stop record first"));
            if (outputFile.Exists) throw (new Exception("Output file already exists"));

            _record = new StreamWriter(outputFile.FullName, false, Encoding.UTF8);
            OnCommandRequested += Prompt_OnCommandRequested;
        }

        /// <summary>
        /// Stop current recording
        /// </summary>
        [PromptCommand("record stop", Help = "Stop current record", Category = "Usability")]
        private void RecordStopCommand()
        {
            if (_record == null) throw (new Exception("Empty record"));

            OnCommandRequested -= Prompt_OnCommandRequested;

            _record.Flush();
            _record.Close();
            _record.Dispose();
            _record = null;
        }

        private void Prompt_OnCommandRequested(IPrompt prompt, PromptCommandAttribute cmd, string commandLine)
        {
            if (cmd.Command == "record stop")
                return;

            _record.WriteLine(commandLine);
        }

        /// <summary>
        /// Clear
        /// </summary>
        [PromptCommand("clear", Help = "clear output", Category = "Usability")]
        public void ClearCommand()
        {
            _consoleWriter.Clear();
        }

        /// <summary>
        /// Load commands from file
        /// </summary>
        /// <param name="commandsFile">File</param>
        [PromptCommand("load", Help = "Play stored commands", Category = "Usability")]
        public void LoadCommand(FileInfo commandsFile)
        {
            if (!commandsFile.Exists)
            {
                _consoleWriter.WriteLine("File not found", ConsoleOutputStyle.Error);
                return;
            }

            if (commandsFile.Length > 1024 * 1024)
            {
                _consoleWriter.WriteLine("The specified file is too large", ConsoleOutputStyle.Error);
                return;
            }

            var lines = File.ReadAllLines(commandsFile.FullName, Encoding.UTF8);
            _consoleReader.AppendInputs(lines.Where(u => !u.StartsWith("#")).ToArray());

            // Print result

            _consoleWriter.WriteLine($"Loaded inputs: {lines.Length}");
        }

        /// <summary>
        /// Exit prompt
        /// </summary>
        [PromptCommand("quit", Category = "Usability")]
        public void QuitCommand([PromptHideHelpCommand] INetworkManager networkManager)
        {
            networkManager?.StopNetwork();
            _exit = true;
        }

        /// <summary>
        /// Exit prompt
        /// </summary>
        [PromptCommand("exit", Category = "Usability")]
        public void ExitCommand([PromptHideHelpCommand] INetworkManager networkManager)
        {
            networkManager?.StopNetwork();
            _exit = true;
        }

        /// <summary>
        /// Show help
        /// </summary>
        [PromptCommand("help", Category = "Usability", Help = "Show help for commands")]
        public void HelpCommand([PromptCommandParameterBody]string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                var cmdArgs = new List<CommandToken>(command.SplitCommandLine());
                var cmds = _commandCache.SearchCommands(cmdArgs).ToArray();

                if (cmds.Length == 0)
                {
                    _consoleWriter.WriteLine("Command not found <" + command + ">", ConsoleOutputStyle.Error);
                }
                else PrintHelp(cmds);

                return;
            }

            HelpCommand();
        }

        /// <summary>
        /// Show help
        /// </summary>
        [PromptCommand("help", Category = "Usability", Help = "Show help for commands")]
        public void HelpCommand()
        {
            string lastCat = null, lastCom = null;
            foreach (var key in _commandCache.Keys.OrderBy(u => _commandCache[u].Category + "\n" + string.Join("", u)))
            {
                var c = _commandCache[key];

                if (lastCat != c.Category)
                {
                    // Print category

                    lastCat = c.Category;
                    _consoleWriter.WriteLine(lastCat, ConsoleOutputStyle.Information);
                }

                var command = string.Join(" ", key);
                if (lastCom == command) continue;

                lastCom = command;
                _consoleWriter.WriteLine("  " + command);
            }
        }

        /*
        TODO
        notifications {block_number or address}
        mem
        config debug {on/off}
        config sc-events {on/off}
        config maxpeers {num_peers}
        debugstorage {on/off/reset} 
        */
    }
}