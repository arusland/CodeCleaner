using System;
using Orygin.Shared.Minimal.Helpers;

namespace CodeCleaner
{
    public class NewProblemEventArgs : EventArgs
    {
        public NewProblemEventArgs(Problem problem)
        {
            Checker.NotNull(problem, "problem");

            Problem = problem;
        }

        public Problem Problem
        {
            get;
            private set;
        }
    }
}
