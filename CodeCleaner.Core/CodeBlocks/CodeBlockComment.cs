
namespace CodeCleaner.CodeBlocks
{
    public class CodeBlockComment : CodeBlock
    {
        #region Ctors

        public CodeBlockComment(string content, int lineNumber, CodeBlock parent)
            :base(string.Empty, CodeBlockType.Comment, content, string.Empty, ModificatorType.Default, new CodeBlock[0], lineNumber, parent)
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
