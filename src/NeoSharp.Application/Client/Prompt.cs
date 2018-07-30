using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoSharp.Application.Attributes;
using NeoSharp.Application.Controllers;
using NeoSharp.Application.Extensions;
using NeoSharp.Core.Blockchain;
using NeoSharp.Core.DI;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Types;
using NeoSharp.VM;

namespace NeoSharp.Application.Client
{
    public partial class Prompt : IPrompt
    {
        #region Variables

        /// <summary>
        /// Exit flag
        /// </summary>
        private bool _exit;
        /// <summary>
        /// Console Reader
        /// </summary>
        private readonly IConsoleReader _consoleReader;
        /// <summary>
        /// Console Writer
        /// </summary>
        private readonly IConsoleWriter _consoleWriter;
        /// <summary>
        /// Logger
        /// </summary>
        private readonly Core.Logging.ILogger<Prompt> _logger;
        /// <summary>
        /// Blockchain
        /// </summary>
        private readonly IBlockchain _blockchain;
        /// <summary>
        /// Command cache
        /// </summary>
        private readonly IDictionary<string[], PromptCommandAttribute> _commandCache;
        /// <summary>
        /// Autocomplete handler
        /// </summary>
        private readonly IAutoCompleteHandler _commandAutocompleteCache;
        /// <summary>
        /// Log for output
        /// </summary>
        private readonly ConcurrentBag<LogEntry> _logs;

        public delegate void delOnCommandRequested(IPrompt prompt, PromptCommandAttribute cmd, string commandLine);
        public event delOnCommandRequested OnCommandRequested;

        private static readonly Dictionary<LogLevel, ConsoleOutputStyle> _logStyle = new Dictionary<LogLevel, ConsoleOutputStyle>()
        {
            { LogLevel.Critical, ConsoleOutputStyle.Error },
            { LogLevel.Error, ConsoleOutputStyle.Error },
            { LogLevel.Information, ConsoleOutputStyle.Log },
            { LogLevel.None, ConsoleOutputStyle.Log },
            { LogLevel.Trace, ConsoleOutputStyle.Log },
            { LogLevel.Warning, ConsoleOutputStyle.Log },
            { LogLevel.Debug, ConsoleOutputStyle.Log }
        };

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="container">Container</param>
        /// <param name="consoleReaderInit">Console reader init</param>
        /// <param name="consoleWriterInit">Console writer init</param>
        /// <param name="logger">Logger</param>
        /// <param name="blockchain">Blockchain</param>
        /// <param name="vmFactory">VM Factory</param>
        public Prompt
            (
            IContainer container,
            PromptControllerFactory controllers,
            IConsoleReader consoleReaderInit,
            IConsoleWriter consoleWriterInit,
            Core.Logging.ILogger<Prompt> logger,
            IBlockchain blockchain
            )
        {
            _consoleReader = consoleReaderInit;
            _consoleWriter = consoleWriterInit;
            _logger = logger;
            _blockchain = blockchain;
            _logs = new ConcurrentBag<LogEntry>();

            // Get controllers

            _commandAutocompleteCache = new AutoCommandComplete();
            _commandCache = new Dictionary<string[], PromptCommandAttribute>();
            _commandCache.Cache(this, _commandAutocompleteCache);

            foreach (var controller in controllers)
            {
                _commandCache.Cache(container.Resolve(controller), _commandAutocompleteCache);
            }
        }

        /// <inheritdoc />
        public void StartPrompt(string[] args)
        {
            _logger.LogInformation("Starting Prompt");
            _consoleWriter.WriteLine("Neo-Sharp", ConsoleOutputStyle.Prompt);

            if (args != null)
            {
                // Append arguments as inputs

                _consoleReader.AppendInputs(args.Where(u => !u.StartsWith("#")).ToArray());
            }

            _blockchain.InitializeBlockchain().Wait();

            while (!_exit)
            {
                // Read log buffer

                while (_logs.TryTake(out var log))
                {
                    _consoleWriter.WriteLine
                        (
                        "[" + log.Level + (string.IsNullOrEmpty(log.Category) ? "" : "-" + log.Category) + "] " +
                        log.MessageWithError, _logStyle[log.Level]
                        );
                }

                // Read input

                var fullCmd = _consoleReader.ReadFromConsole(_commandAutocompleteCache);

                if (string.IsNullOrWhiteSpace(fullCmd))
                {
                    continue;
                }

                _logger.LogInformation("Execute: " + fullCmd);

                Execute(fullCmd);
            }

            _consoleWriter.WriteLine("Exiting", ConsoleOutputStyle.Information);
        }

        /// <inheritdoc />
        public bool Execute(string command)
        {
            command = command.Trim();
            PromptCommandAttribute[] cmds = null;

            try
            {
                // Parse arguments

                var cmdArgs = new List<CommandToken>(command.SplitCommandLine());
                cmds = _commandCache.SearchCommands(cmdArgs).ToArray();
                var cmd = cmds.SearchRightCommand(cmdArgs, null);

                if (cmd == null)
                {
                    if (cmds.Length > 0)
                    {
                        throw (new Exception($"Wrong parameters for <{cmds.FirstOrDefault().Command}>"));
                    }

                    throw (new Exception($"Command not found <{command}>"));
                }

                // Get command

                lock (_consoleReader) lock (_consoleWriter)
                    {
                        // Raise event

                        OnCommandRequested?.Invoke(this, cmd, command);

                        // Invoke

                        var ret = cmd.Method.Invoke(cmd.Instance, cmd.ConvertToArguments(cmdArgs.Skip(cmd.TokensCount).ToArray(), null));

                        if (ret is Task task)
                        {
                            task.Wait();
                        }
                    }

                return true;
            }
            catch (Exception e)
            {
                var msg = e.InnerException != null ? e.InnerException.Message : e.Message;
                _consoleWriter.WriteLine(msg, ConsoleOutputStyle.Error);
#if DEBUG
                _consoleWriter.WriteLine(e.ToString(), ConsoleOutputStyle.Error);
#endif

                PrintHelp(cmds);
                return false;
            }
        }
    }
}