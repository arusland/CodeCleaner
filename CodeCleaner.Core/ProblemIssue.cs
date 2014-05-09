using System;
using Orygin.Shared.Minimal.Helpers;

namespace CodeCleaner
{
    public class ProblemIssue
    {
        #region Ctors

        public ProblemIssue(string message, int lineNumber)
            : this(message, lineNumber, false, IssueType.Normal, null)
        {
        }

        public ProblemIssue(string message, int lineNumber, bool quarantined, Exception parseCause)
            : this(message, lineNumber, quarantined, IssueType.Normal, parseCause)
        {
        }

        public ProblemIssue(string message, int lineNumber, IssueType issueType)
            :this(message, lineNumber, false, issueType, null)
        {
        }

        public ProblemIssue(string message, int lineNumber, bool quarantined, IssueType issueType, Exception parseCause)
        {
            Checker.NotNullOrEmpty(message, "message");
            Checker.AreNotEqual(issueType, IssueType.None);

            Message = message;
            LineNumber = lineNumber;
            Quarantined = quarantined;
            IssueType = issueType;
            ParseCause = parseCause;
        }
        
        #endregion

        #region Properties
        
        #region Public

        public Exception ParseCause
        {
            get;
            private set;
        }

        public String Message
        {
            get;
            private set;
        }

        public int LineNumber
        {
            get;
            private set;
        }

        public bool Quarantined
        {
            get;
            private set;
        }

        public IssueType IssueType
        {
            get;
            private set;
        }

        #endregion
        
        #endregion
    }
}
