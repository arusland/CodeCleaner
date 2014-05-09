using System;

namespace CodeCleaner.CodeBlocks
{
    public class CodeBlockPlainText : CodeBlock
    {
        #region Ctors

        public CodeBlockPlainText(int lineNumber, CodeBlock parent)
            : base(string.Empty, CodeBlockType.PlainText, string.Empty, string.Empty, ModificatorType.Default, new CodeBlock[0], lineNumber, parent)
        {
        }
        
        #endregion

        #region Methods

        #region Public

        public override string Generate(int count)
        {
            throw new NotImplementedException();
        }
        
        #endregion
        
        #endregion
    }
}
