
namespace CodeCleaner
{
    public class CodeBlockDestructor : CodeBlockMethodBase
    {
        #region Ctors

        public CodeBlockDestructor(string name, ModificatorType modificator, string[] arguments,
            string body, string header, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Destructor, modificator, string.Empty, arguments, body, header, preBlocks, lineNumber, parent)
        {
        }

        #endregion
    }
}
