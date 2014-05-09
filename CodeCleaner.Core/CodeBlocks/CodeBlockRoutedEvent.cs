using Orygin.Shared.Minimal.Extensions;
using System.Text;

namespace CodeCleaner
{
    public class CodeBlockRoutedEvent : CodeBlockPropertyBase
    {
        #region Ctors

        public CodeBlockRoutedEvent(string name, ModificatorType modificator, bool isStatic, string rightSide, string header, CodeBlock[] preBlocks, 
            bool isReadonly, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.RoutedEvent, modificator, string.Empty, false, isStatic, false, false,
            false, rightSide, header, preBlocks, string.Empty, lineNumber, parent)
        {
            IsReadonly = isReadonly;
        }

        #endregion

        #region Properties
        
        #region Public

        public bool IsReadonly
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
                return IsReadonly == block.To<CodeBlockRoutedEvent>().IsReadonly;
            }

            return false;
        }

        public override string Generate(int count)
        {
            StringBuilder result = new StringBuilder();

            result.AppendFormat("{1}{0};", RawContent, GetTabs(count));

            return result.ToString();
        }
        
        #endregion
        
        #endregion
    }
}
