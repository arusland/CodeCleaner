using System;

namespace CodeCleaner
{
    public class ProgressCompleteEventArgs : EventArgs
    {
        #region Ctors

        public ProgressCompleteEventArgs(bool canceled, int proccessedFileCount)
        {
            Canceled = canceled;
            ProccessedFileCount = proccessedFileCount;
        }

        public ProgressCompleteEventArgs(Exception error)
            :this(false, 0)
        {
            Error = error;
        }

        #endregion

        #region Properties

        #region Public

        public bool Canceled
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        public int ProccessedFileCount
        {
            get;
            private set;
        }

        #endregion

        #endregion
    }
}
