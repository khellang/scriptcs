using Common.Logging;

using ScriptCs.Contracts;

namespace ScriptCs
{
    public class DebugScriptExecutor : ScriptExecutor
    {
        public DebugScriptExecutor(IFileSystem fileSystem, IScriptProcessor scriptProcessor, IScriptEngine scriptEngine, ILog logger)
            : base(fileSystem, scriptProcessor, scriptEngine, logger)
        {
        }
    }
}
