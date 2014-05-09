using Orygin.Shared.Minimal.Extensions;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockIndexProperty : CodeBlockPropertyBase
    {
        #region Ctors

        public CodeBlockIndexProperty(ModificatorType modificator, string returnType, bool isAbstract, bool isVirtual,
            bool isOverride, bool isNew, string body, string header, string[] arguments, CodeBlock[] preBlocks, string explicitInterfaceName, int lineNumber, CodeBlock parent)
            : base(string.Empty, CodeBlockType.IndexProperty, modificator, returnType, isAbstract, false, isVirtual, isOverride, isNew, body, header,
            preBlocks, explicitInterfaceName, lineNumber, parent)
        {
            Arguments = arguments ?? new string[0];            
        }

        #endregion

        #region Properties
        
        #region Public

        public string[] Arguments
        {
            get;
            private set;
        }       
        
        #endregion
        
        #endregion

        #region Methods
        
        #region Public

        public override bool CompareTo(CodeBlock block)
        {
            if (base.CompareTo(block))
            {
                return Arguments.CompareTo(block.To<CodeBlockIndexProperty>().Arguments);
            }

            return false;
        }       
        
        #endregion
        
        #endregion
    }
}
