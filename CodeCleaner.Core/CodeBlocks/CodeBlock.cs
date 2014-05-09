using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    public abstract class CodeBlock
    {
        #region Fields

        private static string _Tab;
        private static Dictionary<int, string> _Tabs;
        private IList<CodeBlock> _InnerBlocks;
        private IList<CodeBlock> _ValuableInnerBlocks;
        
        #endregion

        #region Ctors

        public CodeBlock(string name, CodeBlockType type, string content, string rawContent, ModificatorType modificator, CodeBlock[] preBlocks, int lineNumber, CodeBlock parent)
        {
            Checker.AreNotEqual(CodeBlockType.None, type);
            Checker.NotNull(content, "content");
            Checker.NotNull(rawContent, "rawContent");
            Checker.NotNull(preBlocks, "preBlocks");

            Name = name;
            LineNumber = lineNumber;
            Type = type;
            Modificator = modificator;
            Content = content.Trim();
            InnerBlocks = new List<CodeBlock>();
            PreBlocks = preBlocks;
            Parent = parent;
            RawContent = rawContent;

            ValidateName(Name);
        }

        static CodeBlock()
        {
            _Tab = "    ";
            _Tabs = new Dictionary<int, string>();
        }

        #endregion

        #region Properties

        #region Public

        public bool IsContainerType
        {
            get
            {
                switch (Type)
                {
                    case CodeBlockType.Namespace:
                    case CodeBlockType.Class:
                    case CodeBlockType.Region:
                    case CodeBlockType.Structure:
                    case CodeBlockType.Interface:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public CodeBlock[] PreBlocks
        {
            get;
            private set;
        }

        public ModificatorType Modificator
        {
            get;
            private set;
        }

        public CodeBlockType Type
        {
            get;
            private set;
        }

        public IList<CodeBlock> InnerBlocks
        {
            get
            {
                return _InnerBlocks;
            }
            set
            {
                Checker.NotNull(value, "value");

                _ValuableInnerBlocks = null;
                _InnerBlocks = value;
            }
        }

        public IList<CodeBlock> ValuableInnerBlocks
        {
            get
            {
                if (_ValuableInnerBlocks.IsNull())
                {
                    _ValuableInnerBlocks = InnerBlocks.Where(p => p.Type != CodeBlockType.Comment && p.Type != CodeBlockType.SingleLineDirective).ToList();
                }

                return _ValuableInnerBlocks;
            }
        }

        public string Content
        {
            get;
            private set;
        }

        public string RawContent
        {
            get;
            private set;
        }

        public int LineNumber
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public bool HasInnerBlocks
        {
            get
            {
                return InnerBlocks.Count > 0;
            }
        }

        public CodeBlock Parent
        {
            get;
            private set;
        }

        public CodeBlock TopTargetBlock
        {
            get
            {
                switch (Type)
                {
                    case CodeBlockType.Namespace:
                    case CodeBlockType.Interface:
                    case CodeBlockType.Class:
                    case CodeBlockType.Structure:
                        return this;
                    default:
                        return Parent.TopTargetBlock;
                }
            }
        }
        
        #endregion

        #region Protected

        protected static string Tab
        {
            get
            {
                return _Tab;
            }
        }
        
        #endregion

        #endregion

        #region Methods

        #region Public

        public string Generate()
        {
            return Generate(0);
        }

        public abstract string Generate(int count);

        public void ValidateName(string name)
        {
            if (name.IsNull())
            {
                throw new ArgumentNullException("Invalid name");
            }

            if (Type != CodeBlockType.IndexProperty && Type != CodeBlockType.Region && Type != CodeBlockType.PlainText && Type != CodeBlockType.Comment
                && Type != CodeBlockType.Using && Type != CodeBlockType.Attribute && Type != CodeBlockType.SingleLineDirective)
            {
                if (name.IsEmpty())
                {
                    throw new ArgumentException("Invalid  name");
                }

                if (Type == CodeBlockType.Namespace)
                {
                    if (!Regex.IsMatch(name, @"^[\w\.]+$"))
                    {
                        throw new ArgumentException("Invalid name: " + name);
                    }
                }
                else if (Type == CodeBlockType.Operator)
                {
                    if (!Regex.IsMatch(name, @"^[\w\=\+\-\*\/\<\>\!]+$"))
                    {
                        throw new ArgumentException("Invalid name: " + name);
                    }
                }
                else if (!Regex.IsMatch(name, @"^\w+$"))
                {
                    throw new ArgumentException("Invalid name: " + name);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("CodeBlock: {0}:{1}", Type.ToString(), Name);
        }

        public static string GetTabs(int count)
        {
            if (count == 1)
            {
                return _Tab;
            }
            else if (!_Tabs.ContainsKey(count))
            {
                StringBuilder result = new StringBuilder();

                for (int i = 0; i < count;i++)
                {
                    result.Append(Tab);
                }

                _Tabs.Add(count, result.ToString());
            }

            return _Tabs[count];
        }

        public virtual bool CompareTo(CodeBlock block)
        {
            if (this == block)
            {
                return true;
            }

            if (Name.IdenticalTo(block.Name) && block.Content.IdenticalTo(Content) && block.Modificator == Modificator && block.RawContent.IdenticalTo(RawContent)
                && block.Type == Type && block.PreBlocks.Length == PreBlocks.Length && block.InnerBlocks.Count == InnerBlocks.Count)
            {
                for (int i = 0; i < PreBlocks.Length;i++ )
                {
                    if (!PreBlocks[i].CompareTo(block.PreBlocks[i]))
                    {
                        return false;
                    }
                }

                for (int i = 0; i < InnerBlocks.Count; i++)
                {
                    if (!InnerBlocks[i].CompareTo(block.InnerBlocks[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
        
        #endregion

        #region Protected

        protected virtual string GenerateChildren(int count)
        {
            return GenerateChildren(count, InnerBlocks, true);
        }

        protected static string GenerateChildren(int count, IEnumerable<CodeBlock> blocks, bool keepDistance)
        {
            StringBuilder result = new StringBuilder();

            if (keepDistance)
            {
                foreach (CodeBlock child in blocks)
                {
                    if (child.Type != CodeBlockType.Comment && child.Type != CodeBlockType.Field && child.Type != CodeBlockType.Const && child.Type != CodeBlockType.SingleLineDirective)
                    {
                        result.AppendFormat("{0}{1}{0}", Environment.NewLine, child.Generate(count));
                    }
                    else
                    {
                        // comment must be closer to element after comment
                        result.AppendFormat("{0}{1}", Environment.NewLine, child.Generate(count));
                    }
                }
            }
            else
            {
                foreach (CodeBlock child in blocks)
                {
                    result.AppendFormat("{1}{0}", Environment.NewLine, child.Generate(count));
                }
            }

            return result.ToString().TrimEnd();
        }
        
        #endregion

        #endregion
    }
}
