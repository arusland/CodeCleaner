using System;
using Orygin.Shared.Minimal.Extensions;

namespace CodeCleaner
{
    public class CodeBlockEnum : CodeBlock
    {
        #region Ctors

        public CodeBlockEnum(string name, ModificatorType modificator, string baseType, string content, string header, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Enum, content, header, modificator, preBlocks, lineNumber, parent)
        {
            BaseType = baseType;
        }

        #endregion

        #region Properties

        #region Public

        public string BaseType
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
                return BaseType == block.To<CodeBlockEnum>().BaseType;
            }

            return false;
        }

        public override string Generate(int count)
        {
            return string.Format("{3}{1}{0}{3}{{{0}{3}{4}{2}{0}{3}}}", Environment.NewLine, RawContent, Content, GetTabs(count), Tab);
        }
        
        #endregion
        
        #endregion
    }
}
