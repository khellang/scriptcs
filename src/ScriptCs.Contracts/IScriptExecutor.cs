using System.Collections.Generic;

namespace ScriptCs.Contracts
{
    public interface IScriptExecutor
    {
        void ImportNamespaces(IEnumerable<string> namespaces);

        void AddReferences(IEnumerable<string> references);

        void RemoveReferences(IEnumerable<string> references);

        void RemoveNamespaces(IEnumerable<string> namespaces);

        void Initialize(IEnumerable<string> paths, IEnumerable<IScriptPack> scriptPacks);

        void Terminate();

        ScriptResult Execute(string path, params string[] scriptArgs);

        ScriptResult ExecuteScript(string script, params string[] scriptArgs);
    }
}