
namespace CodeCleaner.CodeBlocks
{
    public class CodeBlockUsing : CodeBlock
    {
        #region Ctors

        public CodeBlockUsing(string content, int lineNumber, CodeBlock parent)
            : base(string.Empty, CodeBlockType.Using, content, string.Empty, ModificatorType.Default, new CodeBlock[0], lineNumber, parent)
        {
        }
        
        #endregion

        #region Methods

        #region Public

        public override string Generate(int count)
        {
            return string.Format("{0}{1};", GetTabs(count), Content);
        }
        
        #endregion
        
        #endregion
    }
}
