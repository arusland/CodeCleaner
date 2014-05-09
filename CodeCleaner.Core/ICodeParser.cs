using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public interface ICodeParser
    {
        bool Parse(string fileName);

        bool Regenerateable
        {
            get;
        }

        bool HasUnrecognisedBlocks
        {
            get;
        }

        IList<CodeBlock> CodeBlocks
        {
            get;
        }
    }
}
