using System;

namespace CodeCleaner
{
    public class NewProgressChangedEventArgs : EventArgs
    {
        public NewProgressChangedEventArgs(string nextFilename, int percentage)
        {
            NextFilename = nextFilename;
            Percentage = percentage;
        }

        public string NextFilename
        {
            get;
            private set;
        }

        public int Percentage
        {
            get;
            private set;
        }
    }
}
