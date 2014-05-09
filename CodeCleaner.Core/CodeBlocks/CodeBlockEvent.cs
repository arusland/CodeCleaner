using System;
using System.Text;
using Orygin.Shared.Minimal.Extensions;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockEvent : CodeBlockField
    {
        #region Ctors

        public CodeBlockEvent(string name, ModificatorType modificator, string eventType, bool isStatic, string content, string header,
            CodeBlock[] preBlocks, string explicitInterfaceName, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Event, modificator, eventType, string.Empty, isStatic, false, content, header, preBlocks, lineNumber, parent)
        {
            ExplicitInterfaceName = explicitInterfaceName;
        }

        #endregion

        #region Properties

        #region Public

        public string ExplicitInterfaceName
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
                return block.To<CodeBlockEvent>().ExplicitInterfaceName.IdenticalTo(ExplicitInterfaceName);
            }

            return false;
        }

        public override string Generate(int count)
        {
            if (Content.IsNotEmpty())
            {
                StringBuilder result = new StringBuilder();

                result.AppendFormat("{3}{1}{0}{3}{{{0}{3}{4}{2}{0}{3}}}", Environment.NewLine, RawContent, Content, GetTabs(count), Tab);

                return result.ToString();
            }
            else
            {
                return base.Generate(count);
            }
        }
        
        #endregion
        
        #endregion
    }
}
