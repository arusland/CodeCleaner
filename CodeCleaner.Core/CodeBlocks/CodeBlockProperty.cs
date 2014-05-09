
namespace CodeCleaner
{
    public class CodeBlockProperty : CodeBlockPropertyBase
    {
        #region Ctors

        public CodeBlockProperty(string name, ModificatorType modificator, string returnType, bool isAbstract, bool isStatic, bool isVirtual,
            bool isOverride, bool isNew, string body, string header, CodeBlock[] preBlocks, string explicitInterfaceName, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Property, modificator, returnType, isAbstract, isStatic, isVirtual, isOverride,
            isNew, body, header, preBlocks, explicitInterfaceName, lineNumber, parent)
        {
        }

        #endregion
    }
}
