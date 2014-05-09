using System;
using System.Text;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockPropertyBase : CodeBlock
    {
        #region Ctors

        public CodeBlockPropertyBase(string name, CodeBlockType type, ModificatorType modificator, string returnType, bool isAbstract, bool isStatic, bool isVirtual,
            bool isOverride, bool isNew, string body, string header, CodeBlock[] preBlocks, string explicitInterfaceName, int lineNumber, CodeBlock parent)
            : base(name, type, body, header, modificator, preBlocks, lineNumber, parent)
        {
            Checker.NotNull(returnType, "fieldType");

            ReturnType = returnType;
            IsStatic = isStatic;
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

        public bool IsStatic
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
                CodeBlockPropertyBase bProperty = block.To<CodeBlockPropertyBase>();

                return ReturnType.IdenticalTo(bProperty.ReturnType) && IsStatic == bProperty.IsStatic 
                    && bProperty.ExplicitInterfaceName.IdenticalTo(ExplicitInterfaceName);
            }

            return false;
        }

        public override string Generate(int count)
        {
            StringBuilder result = new StringBuilder();

            result.AppendFormat("{3}{1}{0}{3}{{{0}{3}{4}{2}{0}{3}}}", Environment.NewLine, RawContent, Content, GetTabs(count), Tab);

            return result.ToString();
        }
        
        #endregion
        
        #endregion
    }
}
