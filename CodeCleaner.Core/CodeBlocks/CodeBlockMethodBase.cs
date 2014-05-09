using System;
using System.Text;
using Orygin.Shared.Minimal.Extensions;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public abstract class CodeBlockMethodBase : CodeBlock
    {
        #region Ctors

        public CodeBlockMethodBase(string name, CodeBlockType type, ModificatorType modificator, string returnType, string[] arguments, string body,
            string header, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            : base(name, type, body, header, modificator, preBlocks, lineNumber, parent)
        {
            ReturnType = returnType;
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

        public string ReturnType
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
                CodeBlockMethodBase bMethod = block.To<CodeBlockMethodBase>();

                return ReturnType.IdenticalTo(bMethod.ReturnType) && Arguments.CompareTo(bMethod.Arguments);
            }

            return false;
        }

        public override string Generate(int count)
        {
            StringBuilder result = new StringBuilder();

            if (Content.IsNotEmpty())
            {
                result.AppendFormat("{3}{1}{0}{3}{{{0}{3}{4}{2}{0}{3}}}", Environment.NewLine, RawContent, Content, GetTabs(count), Tab);
            }
            else
            {
                result.AppendFormat("{2}{1}{0}{2}{{{0}{2}}}", Environment.NewLine, RawContent, GetTabs(count));
            }

            return result.ToString();
        }

        #endregion

        #endregion
    }
}
