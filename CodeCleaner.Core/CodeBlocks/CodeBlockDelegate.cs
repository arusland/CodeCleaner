using System.Text;
using System;
using Orygin.Shared.Minimal.Extensions;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockDelegate : CodeBlockMethodBase
    {
        #region Ctors

        public CodeBlockDelegate(string name, ModificatorType modificator, string returnType, string[] arguments, string genericPart,
            CodeBlock[] preBlocks, int lineNumber, string header, CodeBlock parent)
            : base(name, CodeBlockType.Delegate, modificator, returnType, arguments, string.Empty, header, preBlocks, lineNumber, parent)
        {
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
        
        #endregion
        
        #endregion

        #region Methods

        #region Public

        public override string Generate(int count)
        {
            StringBuilder result = new StringBuilder();

            result.AppendFormat("{1}{0};", RawContent, GetTabs(count));

            return result.ToString();
        }

        public override bool CompareTo(CodeBlock block)
        {
            return base.CompareTo(block) && block.To<CodeBlockDelegate>().GenericPart.IdenticalTo(GenericPart);
        }

        #endregion

        #endregion
    }
}
