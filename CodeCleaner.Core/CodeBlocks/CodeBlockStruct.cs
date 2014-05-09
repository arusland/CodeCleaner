using System;
using System.Text;
using Orygin.Shared.Minimal.Extensions;


namespace CodeCleaner
{
    public class CodeBlockStruct : CodeBlock
    {
        #region Ctors

        public CodeBlockStruct(string name, ModificatorType modificator, bool isPartial, string content, string header, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Structure, content, header, modificator, preBlocks, lineNumber, parent)
        {
            IsPartial = isPartial;
        }

        #endregion

        #region Properties

        #region Public

        public bool IsPartial
        {
            get;
            private set;
        }

        #endregion

        #endregion

        #region Methods

        public override string Generate(int count)
        {
            StringBuilder result = new StringBuilder();

            result.AppendFormat("{3}{1}{0}{3}{{{2}{0}{3}}}", Environment.NewLine, RawContent, GenerateChildren(count + 1), GetTabs(count));

            return result.ToString();
        }
        
        #endregion

        #region Methods
        
        #region Public

        public override bool CompareTo(CodeBlock block)
        {
            if (base.CompareTo(block))
            {
                return IsPartial == block.To<CodeBlockStruct>().IsPartial;
            }

            return false;
        }
        
        #endregion
        
        #endregion
    }
}
