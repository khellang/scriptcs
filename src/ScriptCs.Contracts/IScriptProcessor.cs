namespace ScriptCs.Contracts
{
    public interface IScriptProcessor : IScriptParser
    {
        ScriptProcessorResult ProcessFile(string path);

        ScriptProcessorResult ProcessScript(string script);
    }
}