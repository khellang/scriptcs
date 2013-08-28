namespace ScriptCs.Contracts
{
    public interface IScriptParser
    {
        void ParseFile(string path, ScriptParserContext context);

        void ParseScript(string script, ScriptParserContext context);
    }
}