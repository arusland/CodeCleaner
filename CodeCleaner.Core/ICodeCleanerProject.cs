using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeCleaner
{
    public interface ICodeCleanerProject
    {
        /// <summary>
        /// RegEx patterns for name of files need to exclude
        /// </summary>
        IList<string> ExcludeFilenamePatterns
        {
            get;
        }

        /// <summary>
        /// RegEx patterns for content of files need to exclude
        /// </summary>
        IList<string> ExcludeContentPatterns
        {
            get;
        }

        /// <summary>
        /// RegEx patterns for name of files need to include
        /// </summary>
        IList<string> IncludeFilenamePatterns
        {
            get;
        }

        /// <summary>
        /// RegEx patterns for content of files need to include
        /// </summary>
        IList<string> IncludeContentPatterns
        {
            get;
        }

        /// <summary>
        /// Folders where files will be find
        /// </summary>
        IList<string> FilesSearchPaths
        {
            get;
        }

        /// <summary>
        /// Folder where files which dint pass CodeParser
        /// </summary>
        string QuarantineOutputPath
        {
            get;
        }

        string CodeSpecificationPath
        {
            get;
        }

        string BackUpOutputPath
        {
            get;
        }
    }
}
