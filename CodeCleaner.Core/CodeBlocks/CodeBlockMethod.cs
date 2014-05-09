using System.Text;
using Orygin.Shared.Minimal.Extensions;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public class CodeBlockMethod : CodeBlockMethodBase
    {
        #region Ctors

        public CodeBlockMethod(string name, ModificatorType modificator, bool isAbstract, bool isStatic, bool isVirtual,
            bool isOverride, bool isNew, bool isPartial, string returnType, string[] arguments, string body, string header, string genericPart,
            CodeBlock[] preBlocks, string explicitInterfaceName, int lineNumber, CodeBlock parent)
            : base(name, CodeBlockType.Method, modificator, returnType, arguments, body, header, preBlocks, lineNumber, parent)
        {
            IsAbstract = isAbstract;
            IsStatic = isStatic;
            IsVirtual = isVirtual;
            IsOverride = isOverride;
            IsNew = isNew;
            IsPartial = isPartial;
            GenericPart = genericPart;
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

        public string GenericPart
        {
            get;
            private set;
        }

        public bool IsAbstract
        {
            get;
            private set;
        }

        public bool IsNew
        {
            get;
            private set;
        }

        public bool IsOverride
        {
            get;
            private set;
        }

        public bool IsPartial
        {
            get;
            private set;
        }

        public bool IsStatic
        {
            get;
            private set;
        }

        public bool IsVirtual
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
                CodeBlockMethod bMethod = block.To<CodeBlockMethod>();

                return bMethod.GenericPart.IdenticalTo(GenericPart) && bMethod.IsAbstract == IsAbstract && bMethod.IsNew == IsNew
                    && bMethod.IsOverride == IsOverride && bMethod.IsPartial == IsPartial && bMethod.IsStatic == IsStatic
                    && bMethod.IsVirtual == IsVirtual && bMethod.ExplicitInterfaceName.IdenticalTo(ExplicitInterfaceName);
            }

            return false;
        }

        public override string Generate(int count)
        {
            if (IsAbstract)
            {
                StringBuilder result = new StringBuilder();

                result.AppendFormat("{1}{0};", RawContent, GetTabs(count));

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
