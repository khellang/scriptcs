namespace ScriptCs.Contracts
{
    public interface ILineProcessor
    {
        bool ProcessLine(IScriptParser parser, ScriptParserContext context, string line, bool isBeforeCode);
    }
}