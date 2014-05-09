
namespace CodeCleaner.CodeBlocks
{
    public class CodeBlockDirective : CodeBlock
    {
        #region Ctors

        public CodeBlockDirective(string content, int lineNumber, CodeBlock parent)
            : base(string.Empty, CodeBlockType.SingleLineDirective, content, string.Empty, ModificatorType.Default, new CodeBlock[0], lineNumber, parent)
        {
        }
        
        #endregion

        #region Methods

        public override string Generate(int count)
        {
            return string.Format("{0}{1}", GetTabs(count), Content);
        }
        
        #endregion
    }
}
