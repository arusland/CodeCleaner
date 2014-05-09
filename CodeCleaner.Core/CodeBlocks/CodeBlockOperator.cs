using System.Text;
using Orygin.Shared.Minimal.Extensions;

namespace CodeCleaner
{
    public class CodeBlockOperator : CodeBlockMethodBase
    {
        #region Ctors

        public CodeBlockOperator(string name, ModificatorType modificator, bool isImplicit, string returnType, string[] arguments, string body, string header,
            CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Operator, modificator, returnType, arguments, body, header, preBlocks, lineNumber, parent)
        {
            IsImplicit = isImplicit;
        }

        #endregion

        #region Properties

        #region Public

        public bool IsImplicit
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
                return block.To<CodeBlockOperator>().IsImplicit == IsImplicit;
            }

            return false;
        }

        #endregion

        #endregion
    }
}
