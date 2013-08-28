using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Common.Logging;

using ScriptCs.Contracts;

namespace ScriptCs
{
    public class ScriptProcessor : IScriptProcessor
    {
        private readonly ILog _logger;

        private readonly IEnumerable<ILineProcessor> _lineProcessors;

        private readonly IEnumerable<IFileBodyProcessor> _fileBodyProcessors;

        private readonly IFileSystem _fileSystem;

        public ScriptProcessor(
            IFileSystem fileSystem,
            ILog logger,
            IEnumerable<ILineProcessor> lineProcessors,
            IEnumerable<IFileBodyProcessor> fileBodyProcessors)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _lineProcessors = lineProcessors;
            _fileBodyProcessors = fileBodyProcessors;
        }

        /// <summary>
        /// Parses and processes the file at the given path.
        /// </summary>
        /// <param name="path">The file to process.</param>
        /// <returns>The processing result containing imported namespaces, 
        /// loaded script paths, references and resulting code.</returns>
        public ScriptProcessorResult ProcessFile(string path)
        {
            return Process(context => ParseFile(path, context));
        }

        /// <summary>
        /// Parses and processes the given script.
        /// </summary>
        /// <param name="script">The script to process.</param>
        /// <returns>The processing result containing imported namespaces, 
        /// loaded script paths, references and resulting code.</returns>
        public ScriptProcessorResult ProcessScript(string script)
        {
            return Process(context => ParseScript(script, context));
        }

        /// <summary>
        /// Parses the file at the given path, adding imported namespaces, 
        /// references, loaded script paths and code lines to the given context.
        /// </summary>
        /// <param name="path">The file to parse.</param>
        /// <param name="context">The context.</param>
        public void ParseFile(string path, ScriptParserContext context)
        {
            Guard.AgainstNullArgument("path", path);
            Guard.AgainstNullArgument("context", context);

            var fullPath = _fileSystem.GetFullPath(path);
            var filename = Path.GetFileName(path);

            if (context.LoadedScripts.Contains(fullPath))
            {
                _logger.DebugFormat("Skipping {0} because it's already been loaded.", filename);
                return;
            }

            _logger.DebugFormat("Processing {0}...", filename);

            // Add script to loaded collection before parsing to avoid loop.
            context.LoadedScripts.Add(fullPath);

            var scriptLines = _fileSystem.ReadFileLines(fullPath).ToList();
            
            InsertLineDirective(fullPath, scriptLines);
            InDirectory(fullPath, () => ParseScriptLines(scriptLines, context));

            ProcessBody(context.BodyLines, fullPath);
        }

        /// <summary>
        /// Parses the file at the given path, adding imported namespaces, 
        /// references, loaded script paths and code lines to the given context.
        /// </summary>
        /// <param name="script">The file to parse.</param>
        /// <param name="context">The context.</param>
        public void ParseScript(string script, ScriptParserContext context)
        {
            Guard.AgainstNullArgument("script", script);
            Guard.AgainstNullArgument("context", context);

            var scriptLines = _fileSystem.SplitLines(script).ToList();

            ParseScriptLines(scriptLines, context);
        }

        private ScriptProcessorResult Process(Action<ScriptParserContext> parseAction)
        {
            Guard.AgainstNullArgument("parseAction", parseAction);

            var context = new ScriptParserContext();

            _logger.DebugFormat("Starting pre-processing");

            parseAction(context);

            var code = GenerateCode(context);

            _logger.DebugFormat("Pre-processing finished successfully");

            return new ScriptProcessorResult
            {
                Namespaces = context.Namespaces,
                LoadedScripts = context.LoadedScripts,
                References = context.References,
                Code = code
            };
        }

        private string GenerateCode(ScriptParserContext context)
        {
            Guard.AgainstNullArgument("context", context);

            var stringBuilder = new StringBuilder();

            var usingLines = context.Namespaces
                .Where(ns => !string.IsNullOrWhiteSpace(ns))
                .Select(ns => string.Format("using {0};", ns))
                .ToList();

            if (usingLines.Count > 0)
            {
                stringBuilder.AppendLine(string.Join(_fileSystem.NewLine, usingLines));
                stringBuilder.AppendLine(); // Insert a blank separator line
            }

            stringBuilder.Append(string.Join(_fileSystem.NewLine, context.BodyLines));

            return stringBuilder.ToString();
        }

        private static void InsertLineDirective(string path, List<string> fileLines)
        {
            Guard.AgainstNullArgument("fileLines", fileLines);

            var bodyIndex = fileLines.FindIndex(line => IsNonDirectiveLine(line) && !IsUsingLine(line));
            if (bodyIndex == -1) return;

            fileLines.Insert(bodyIndex, string.Format("#line {0} \"{1}\"", bodyIndex + 1, path));
        }

        private void ParseScriptLines(List<string> scriptLines, ScriptParserContext context)
        {
            var codeIndex = scriptLines.FindIndex(IsNonDirectiveLine);

            for (var index = 0; index < scriptLines.Count; index++)
            {
                var line = scriptLines[index];
                var isBeforeCode = index < codeIndex || codeIndex < 0;

                var wasProcessed = ProcessLine(context, line, isBeforeCode);
                if (wasProcessed) continue;

                context.BodyLines.Add(line);
            }
        }

        private bool ProcessLine(ScriptParserContext context, string line, bool isBeforeCode)
        {
            return _lineProcessors.Any(x => x.ProcessLine(this, context, line, isBeforeCode));
        }

        private void ProcessBody(List<string> bodyLines, string fullPath)
        {
            foreach (var bodyProcessor in _fileBodyProcessors)
            {
                var wasProcessed = bodyProcessor.ProcessBody(bodyLines, fullPath);
                if (wasProcessed) return;
            }
        }

        private void InDirectory(string path, Action action)
        {
            var oldCurrentDirectory = _fileSystem.CurrentDirectory;
            _fileSystem.CurrentDirectory = _fileSystem.GetWorkingDirectory(path);

            action();

            _fileSystem.CurrentDirectory = oldCurrentDirectory;
        }

        private static bool IsNonDirectiveLine(string line)
        {
            var trimmedLine = line.TrimStart(' ');
            return !trimmedLine.StartsWith("#r ") && !trimmedLine.StartsWith("#load ") && line.Trim() != string.Empty;
        }

        private static bool IsUsingLine(string line)
        {
            return line.TrimStart(' ').StartsWith("using ") && !line.Contains("{") && line.Contains(";");
        }
    }
}
