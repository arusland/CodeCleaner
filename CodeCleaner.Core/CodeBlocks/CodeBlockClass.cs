using System;
using System.Text;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockClass : CodeBlock
    {
        #region Ctors

        public CodeBlockClass(string name, ModificatorType modificator, string[] inheritList, bool isPartial,
            bool isStatic, bool isAbstract, bool isSealed, string content, string header, string genericPart, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            :base(name, CodeBlockType.Class, content, header, modificator, preBlocks, lineNumber, parent)
        {
            Checker.NotNull(inheritList, "inheritList");

            IsPartial = isPartial;
            IsStatic = isStatic;
            IsSealed = isSealed;
            IsAbstract = isAbstract;
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

        public bool IsAbstract
        {
            get;
            private set;
        }

        public bool IsPartial
        {
            get;
            private set;
        }

        public bool IsSealed
        {
            get;
            private set;
        }

        public bool IsStatic
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
                CodeBlockClass bClass = block.To<CodeBlockClass>();

                if (bClass.GenericPart.IdenticalTo(GenericPart) && bClass.IsPartial == IsPartial && bClass.IsSealed == IsSealed
                    && bClass.IsStatic == IsStatic && bClass.IsAbstract == IsAbstract)
                {
                    return bClass.InheritList.CompareTo(InheritList);
                }
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
