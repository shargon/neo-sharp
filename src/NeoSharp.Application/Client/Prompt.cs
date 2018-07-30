using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoSharp.Application.Attributes;
using NeoSharp.Application.Extensions;
using NeoSharp.BinarySerialization;
using NeoSharp.Core.Blockchain;
using NeoSharp.Core.Blockchain.Processors;
using NeoSharp.Core.DI;
using NeoSharp.Core.Extensions;
using NeoSharp.Core.Logging;
using NeoSharp.Core.Network;
using NeoSharp.Core.Network.Rpc;
using NeoSharp.Core.Types;
using NeoSharp.Core.Wallet;
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
        /// VM Factory
        /// </summary>
        private readonly IVMFactory _vmFactory;
        /// <summary>
        /// Logger
        /// </summary>
        private readonly Core.Logging.ILogger<Prompt> _logger;
        /// <summary>
        /// Server
        /// </summary>
        private readonly IServer _server;
        /// <summary>
        /// Blockchain
        /// </summary>
        private readonly IBlockchain _blockchain;

        /// <summary>
        /// The wallet.
        /// </summary>
        private readonly IWalletManager _walletManager;
        /// <summary>
        /// Container
        /// </summary>
        private readonly IContainer _container;
        /// <summary>
        /// Command cache
        /// </summary>
        private static readonly IDictionary<string[], PromptCommandAttribute> _commandCache;
        private static readonly IAutoCompleteHandler _commandAutocompleteCache;

        public delegate void delOnCommandRequested(IPrompt prompt, PromptCommandAttribute cmd, string commandLine);
        public event delOnCommandRequested OnCommandRequested;

        private readonly ConcurrentBag<LogEntry> _logs;

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

        #region Cache

        /// <summary>
        /// Static constructor
        /// </summary>
        static Prompt()
        {
            _commandCache = new Dictionary<string[], PromptCommandAttribute>();
            _commandCache.Cache(typeof(Prompt), out _commandAutocompleteCache);
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="container">Container</param>
        /// <param name="consoleReaderInit">Console reader init</param>
        /// <param name="consoleWriterInit">Console writer init</param>
        /// <param name="logger">Logger</param>
        /// <param name="serverInit">Server</param>
        /// <param name="blockchain">Blockchain</param>
        /// <param name="walletManager"></param>
        /// <param name="vmFactory">VM Factory</param>
        public Prompt(
            IContainer container,
            IConsoleReader consoleReaderInit,
            IConsoleWriter consoleWriterInit,
            Core.Logging.ILogger<Prompt> logger,
            IServer serverInit,
            IBlockchain blockchain,
            IWalletManager walletManager,
            IVMFactory vmFactory)
        {
            _container = container;
            _consoleReader = consoleReaderInit;
            _consoleWriter = consoleWriterInit;
            _logger = logger;
            _server = serverInit;
            _blockchain = blockchain;
            _logs = new ConcurrentBag<LogEntry>();
            _walletManager = walletManager;
            _vmFactory = vmFactory;
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
                var cmd = cmds.SearchRightCommand(cmdArgs, _container);

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

                        var ret = cmd.Method.Invoke(this, cmd.ConvertToArguments(cmdArgs.Skip(cmd.CommandLength).ToArray(), _container));

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