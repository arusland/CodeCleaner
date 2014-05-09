using Orygin.Shared.Minimal.Extensions;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockConstructor : CodeBlockMethodBase
    {
        #region Ctors

        public CodeBlockConstructor(string name, ModificatorType modificator, bool isStatic, string[] arguments, string additionalCallName,
            string body, string header, string genericPart, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Constructor, modificator, string.Empty, arguments, body, header, preBlocks, lineNumber, parent)
        {
            IsStatic = isStatic;
            AdditionalCallName = additionalCallName;
            GenericPart = genericPart;
        }
        
        #endregion

        #region Properties
        
        #region Public

        public string GenericPart
        {
            get;
            private set;
        }

        public bool IsStatic
        {
            get;
            private set;
        }

        public string AdditionalCallName
        {
            get;
            private set;
        }

        public override bool CompareTo(CodeBlock block)
        {
            if (base.CompareTo(block))
            {
                CodeBlockConstructor bCtr = block.To<CodeBlockConstructor>();

                return bCtr.GenericPart.IdenticalTo(GenericPart) && bCtr.IsStatic == IsStatic && AdditionalCallName.IdenticalTo(bCtr.AdditionalCallName);
            }

            return false;
        }

        #endregion
        
        #endregion
    }
}
