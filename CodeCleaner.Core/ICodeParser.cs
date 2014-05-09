using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public interface ICodeParser
    {
        IList<CodeBlock> CodeBlocks
        {
            get;
        }

        bool HasUnrecognisedBlocks
        {
            get;
        }

        bool Regenerateable
        {
            get;
        }

        bool Parse(string fileName);
    }
}
