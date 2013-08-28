using System.Collections.Generic;

namespace ScriptCs.Contracts
{
    public interface IFileBodyProcessor 
    {
        bool ProcessBody(List<string> body, string fullPath);
    }
}