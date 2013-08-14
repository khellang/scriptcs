using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Common.Logging;
using ScriptCs.Contracts;

namespace ScriptCs
{
    public class ScriptExecutor : IScriptExecutor
    {
        public static readonly string[] DefaultReferences =
        {
            "System", 
            "System.Xml", 
            "System.Core", 
            "System.Data", 
            "System.Xml.Linq",
            "System.Data.DataSetExtensions" 
        };

        public static readonly string[] DefaultNamespaces =
        {
            "System",
            "System.IO",
            "System.Linq", 
            "System.Text", 
            "System.Threading.Tasks", 
            "System.Collections.Generic" 
        };

        public ScriptPackSession ScriptPackSession;

        public ScriptExecutor(
            IFileSystem fileSystem,
            IFilePreProcessor filePreProcessor,
            IScriptEngine scriptEngine,
            ILog logger)
        {
            References = new Collection<string>(DefaultReferences);
            Namespaces = new Collection<string>(DefaultNamespaces);
            FileSystem = fileSystem;
            FilePreProcessor = filePreProcessor;
            ScriptEngine = scriptEngine;
            Logger = logger;
        }

        public Collection<string> References { get; private set; }

        public Collection<string> Namespaces { get; private set; }

        public IFileSystem FileSystem { get; private set; }

        public IFilePreProcessor FilePreProcessor { get; private set; }

        public IScriptEngine ScriptEngine { get; private set; }

        public ILog Logger { get; private set; }

        public void ImportNamespaces(IEnumerable<string> namespaces)
        {
            Guard.AgainstNullArgument("namespaces", namespaces);

            foreach (var @namespace in namespaces)
            {
                Namespaces.Add(@namespace);
            }
        }


        public void AddReferences(IEnumerable<string> paths)
        {
            Guard.AgainstNullArgument("paths", paths);

            foreach(var path in paths)
            {
                References.Add(path);
            }
        }

        public void RemoveReferences(IEnumerable<string> paths)
        {
            Guard.AgainstNullArgument("paths", paths);
            
            foreach (var path in paths)
            {
                References.Remove(path);
            }
        }

        public void RemoveNamespaces(IEnumerable<string> namespaces)
        {
            Guard.AgainstNullArgument("namespaces", namespaces);

            foreach (var @namespace in namespaces)
            {
                Namespaces.Remove(@namespace);
            }
        }

        public virtual void Initialize(IEnumerable<string> paths, IEnumerable<IScriptPack> scriptPacks)
        {
            AddReferences(paths.ToArray());
            var bin = Path.Combine(FileSystem.CurrentDirectory, "bin");

            ScriptEngine.BaseDirectory = bin;

            Logger.Debug("Initializing script packs");
            ScriptPackSession = new ScriptPackSession(scriptPacks);

            ScriptPackSession.InitializePacks();
        }

        public virtual void Terminate()
        {
            Logger.Debug("Terminating packs");
            ScriptPackSession.TerminatePacks();
        }

        public virtual ScriptResult Execute(string script, params string[] scriptArgs)
        {
            var rootedPath = Path.IsPathRooted(script) ? script : Path.Combine(FileSystem.CurrentDirectory, script);
            return ExecuteInternal(() => FilePreProcessor.ProcessFile(rootedPath), scriptArgs);
        }

        public virtual ScriptResult ExecuteScript(string script, params string[] scriptArgs)
        {
            return ExecuteInternal(() => FilePreProcessor.ProcessScript(script), scriptArgs);
        }

        private ScriptResult ExecuteInternal(Func<FilePreProcessorResult> process, params string[] scriptArgs)
        {
            var result = process();

            var references = References.Union(result.References);
            var namespaces = Namespaces.Union(result.Namespaces);

            Logger.Debug("Starting execution in engine");
            return ScriptEngine.Execute(result.Code, scriptArgs, references, namespaces, ScriptPackSession);
        }
    }
}