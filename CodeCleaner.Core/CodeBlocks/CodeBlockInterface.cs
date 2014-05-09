using System;
using System.Text;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockInterface : CodeBlock
    {
        #region Ctors

        public CodeBlockInterface(string name, ModificatorType modificator, string[] inheritList, bool isPartial,
            string content, string header, string genericPart, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Interface, content, header, modificator, preBlocks, lineNumber, parent)
        {
            Checker.NotNull(inheritList, "inheritList");

            IsPartial = isPartial;
            InheritList = inheritList;
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

        public string[] InheritList
        {
            get;
            private set;
        }

        public bool IsPartial
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
                CodeBlockInterface bInter = block.To<CodeBlockInterface>();

                return InheritList.CompareTo(bInter.InheritList) && bInter.IsPartial == IsPartial && bInter.GenericPart.IdenticalTo(GenericPart);
            }

            return false;
        }

        public override string Generate(int count)
        {
            StringBuilder result = new StringBuilder();

            result.AppendFormat("{3}{1}{0}{3}{{{2}{0}{3}}}", Environment.NewLine, RawContent, GenerateChildren(count + 1), GetTabs(count));

            return result.ToString();
        }
        
        #endregion
        
        #endregion
    }
}
