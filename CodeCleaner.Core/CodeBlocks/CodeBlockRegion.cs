using System;
using System.Text;

namespace CodeCleaner.CodeBlocks
{
    public class CodeBlockRegion : CodeBlock
    {
        #region Ctors

        public CodeBlockRegion(string name, string content, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Region, content, string.Empty, ModificatorType.Default, new CodeBlock[0], lineNumber, parent)
        {
        }
        
        #endregion

        #region Methods

        public override string Generate(int count)
        {
            StringBuilder result = new StringBuilder();

            result.AppendFormat("{2}#region {3}{0}{1}{0}{0}{2}#endregion", Environment.NewLine, GenerateChildren(count), GetTabs(count), Name);

            return result.ToString();
        }
        
        #endregion
    }
}
