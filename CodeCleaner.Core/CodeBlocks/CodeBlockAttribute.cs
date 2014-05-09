
namespace CodeCleaner
{
    public class CodeBlockAttribute : CodeBlock
    {
        #region Ctors

        public CodeBlockAttribute(string content, int lineNumber, CodeBlock parent)
            :base(string.Empty, CodeBlockType.Attribute, content, string.Empty, ModificatorType.Default, new CodeBlock[0], lineNumber, parent)
        {
        }
        
        #endregion

        #region Methods

        public override string Generate(int count)
        {
            return string.Format("[{0}]", Content);
        }
        
        #endregion
    }
}
