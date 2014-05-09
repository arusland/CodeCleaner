using System;
using System.Collections.Generic;
using System.Linq;
using Orygin.Shared.Minimal.Helpers;

namespace CodeCleaner
{
    public class Problem
    {
        #region Ctors

        public Problem(string filename)
            :this(filename, null, false)
        {
        }

        public Problem(string filename, bool regenerateable)
            : this(filename, null, regenerateable)
        {
        }

        public Problem(string filename, CodeBlock rootBlock, bool regenerateable)
        {
            Checker.NotNullOrEmpty(filename, "filename");

            Filename = filename;
            Issues = new List<ProblemIssue>();
            RootBlock = rootBlock;
            Regenerateable = regenerateable;
        }
        
        #endregion

        #region Properties
        
        #region Public

        public Exception ParseCause
        {
            get
            {
                return Issues.Select(p => p.ParseCause).FirstOrDefault();
            }
        }

        public string Filename
        {
            get;
            private set;
        }

        public bool Quarantined
        {
            get
            {
                return Issues.Any(p => p.Quarantined);
            }
        }

        public bool HasOrderIssue
        {
            get
            {
                return Issues.Any(p => p.IssueType == IssueType.Order);
            }
        }

        public bool HasXamlStrings
        {
            get
            {
                return Issues.Any(p => p.IssueType == IssueType.XamlHardcodedStrings);
            }
        }

        public IList<ProblemIssue> Issues
        {
            get;
            private set;
        }

        public CodeBlock RootBlock
        {
            get;
            private set;
        }

        public bool Regenerateable
        {
            get;
            private set;
        }

        public bool CanFixProblem
        {
            get
            {
                return (HasOrderIssue || HasXamlStrings) && Regenerateable;
            }
        }

        #endregion
        
        #endregion
    }
}
