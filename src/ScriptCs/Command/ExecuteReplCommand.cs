using System;
using System.Threading;
using Common.Logging;
using ScriptCs.Contracts;

namespace ScriptCs.Command
{
    internal class ExecuteReplCommand : IScriptCommand
    {
        private readonly IScriptPackResolver _scriptPackResolver;
        private readonly IAssemblyResolver _assemblyResolver;
        private readonly IFilePreProcessor _filePreProcessor;
        private readonly IObjectSerializer _serializer;
        private readonly IScriptEngine _scriptEngine;
        private readonly string _scriptName;
        private readonly string[] _scriptArgs;
        private readonly IFileSystem _fileSystem;
        private readonly IConsole _console;
        private readonly ILog _logger;

        public ExecuteReplCommand(
            string scriptName,
            string[] scriptArgs,
            IFileSystem fileSystem,
            IScriptPackResolver scriptPackResolver,
            IScriptEngine scriptEngine,
            IFilePreProcessor filePreProcessor,
            IObjectSerializer serializer,
            ILog logger,
            IConsole console,
            IAssemblyResolver assemblyResolver)
        {
            _scriptName = scriptName;
            _scriptArgs = scriptArgs;
            _fileSystem = fileSystem;
            _scriptPackResolver = scriptPackResolver;
            _scriptEngine = scriptEngine;
            _filePreProcessor = filePreProcessor;
            _serializer = serializer;
            _logger = logger;
            _console = console;
            _assemblyResolver = assemblyResolver;
        }

        public string[] ScriptArgs
        {
            get { return _scriptArgs; }
        }

        public CommandResult Execute()
        {
            _console.WriteLine("scriptcs (ctrl-c to exit)" + Environment.NewLine);
            var repl = new Repl(_scriptArgs, _fileSystem, _scriptEngine, _serializer, _logger, _console, _filePreProcessor);

            var workingDirectory = _fileSystem.CurrentDirectory;
            var assemblies = _assemblyResolver.GetAssemblyPaths(workingDirectory);
            var scriptPacks = _scriptPackResolver.GetPacks();

            repl.Initialize(assemblies, scriptPacks, ScriptArgs);

            try
            {
                if (!string.IsNullOrWhiteSpace(_scriptName))
                {
                    _logger.Info(string.Format("Loading script: {0}", _scriptName));
                    repl.Execute(string.Format("#load {0}", _scriptName));
                }

                var shouldQuit = false;

                // We'll handle the exiting ourselves.
                _console.CancelKeyPress += (sender, args) => args.Cancel = true;

                while (true)
                {
                    _console.Write(string.IsNullOrWhiteSpace(repl.Buffer) ? "> " : "* ");

                    try
                    {
                        var line = _console.ReadLine();

                        if (line == null)
                        {
                            if (!shouldQuit)
                            {
                                shouldQuit = true;
                                _console.WriteLine("\n(^C again to quit)");
                                continue; // First Ctrl+C
                            }

                            break; // Second Ctrl+C
                        }

                        if (line.Trim().Length > 0)
                        {
                            repl.Execute(line);
                        }

                        shouldQuit = false; // We'll reset our flag when we've executed some code.
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return CommandResult.Error;
            }

            repl.Terminate();
            return CommandResult.Success;
        }
    }
}
