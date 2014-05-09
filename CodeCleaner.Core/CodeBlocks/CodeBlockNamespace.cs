using System;
using System.Text;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner.Extentions;

namespace CodeCleaner.CodeBlocks
{
    public class CodeBlockNamespace : CodeBlock
    {
        #region Ctors

        public CodeBlockNamespace(string name, string content, string header, string bottom, int lineNumber, CodeBlock[] preBlocks, CodeBlock parent)
            : base(name, CodeBlockType.Namespace, content, header, ModificatorType.Default, preBlocks, lineNumber, parent)
        {
            Checker.NotNull(bottom, "bottom");

            Bottom = bottom;
        }
        
        #endregion

        #region Properties

        public string Header
        {
            get
            {
                return base.RawContent;
            }
        }

        public string Bottom
        {
            get;
            private set;
        }
        
        #endregion

        #region Methods

        public override bool CompareTo(CodeBlock block)
        {
            if (base.CompareTo(block))
            {
                return block.To<CodeBlockNamespace>().Bottom.IdenticalTo(Bottom);
            }

            return false;
        }

        public override string Generate(int count)
        {
            StringBuilder result = new StringBuilder();

            if (PreBlocks.Length > 0)
            {
                result.AppendFormat("{1}{0}{0}", Environment.NewLine, GenerateChildren(count, PreBlocks, false));
            }

            result.Append(Header);
            result.AppendFormat("{0}{2}{{{1}{0}{2}}}{3}", Environment.NewLine, GenerateChildren(count + 1), GetTabs(count), Bottom);

            return result.ToString();
        }
        
        #endregion
    }
}
