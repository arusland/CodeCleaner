﻿namespace CodeCleaner
{
    public interface IFileObserverManager
    {
        /// <summary>
        /// Is file changed or new.
        /// </summary>
        bool IsChanged(string filePath);

        void RemoveFile(string filePath);

        void Save();

        void SetFile(string filePath);
    }
}
