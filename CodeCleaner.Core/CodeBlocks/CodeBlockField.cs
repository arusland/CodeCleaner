using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockField : CodeBlock
    {
        #region Ctors

        public CodeBlockField(string name, CodeBlockType type, ModificatorType modificator,
            string fieldType, string rightSide, bool isStatic, bool isReadonly, string content, string header, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
            : base(name, type, content, header, modificator, preBlocks, lineNumber, parent)
        {
            Checker.NotNullOrEmpty(fieldType, "fieldType");

            if (type == CodeBlockType.Const)
            {
                Checker.AreNotEqual(isStatic, true);
            }

            RightSide = rightSide;
            IsReadonly = isReadonly;
            FieldType = fieldType;
            IsStatic = isStatic;
        }

        #endregion

        #region Properties

        #region Public

        public string FieldType
        {
            get;
            private set;
        }

        public bool IsReadonly
        {
            get;
            private set;
        }

        public bool IsStatic
        {
            get;
            private set;
        }

        public string RightSide
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
                CodeBlockField bField = block.To<CodeBlockField>();

                return bField.IsStatic == IsStatic && bField.IsReadonly == IsReadonly && bField.FieldType == FieldType && bField.RightSide.IdenticalTo(RightSide);
            }

            return false;
        }

        public override string Generate(int count)
        {
            return string.Format("{0}{1};", GetTabs(count), RawContent);
        }

        #endregion

        #endregion
    }
}
