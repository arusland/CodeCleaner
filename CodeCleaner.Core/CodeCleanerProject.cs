using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orygin.Shared.Minimal.Helpers;
using System.Xml;
using Orygin.Shared.Minimal.Extensions;
using System.IO;
using CodeCleaner.Helpers;

namespace CodeCleaner
{
    public class CodeCleanerProject : ICodeCleanerProject
    {
        #region Ctors

        public CodeCleanerProject(string fileName)
        {
            Checker.NotNullOrEmpty(fileName, "fileName");

            Load(fileName);
        }

        #endregion

        #region Methods

        #region Private

        private static string GetAttribute(XmlNode node, string name)
        {
            XmlAttribute attr = node.Attributes[name];

            if (attr.IsNotNull() && attr.Value.IsNotNullOrEmpty())
            {
                return attr.Value;
            }

            throw new InvalidOperationException(string.Format("Attribute '{0}' not found in node '{1}'", name, node.Name));
        }

        private void Load(string fileName)
        {
            XmlDocument doc = new XmlDocument();
            string fileDir = Path.GetDirectoryName(fileName);

            doc.Load(fileName);
            ExcludeFilenamePatterns = LoadValues(doc.DocumentElement.SelectNodes("ExcludeList/FilenamePatterns/Pattern"));
            IncludeFilenamePatterns = LoadValues(doc.DocumentElement.SelectNodes("IncludeList/FilenamePatterns/Pattern"));
            ExcludeContentPatterns = LoadValues(doc.DocumentElement.SelectNodes("ExcludeList/ContentPatterns/Pattern"));
            IncludeContentPatterns = LoadValues(doc.DocumentElement.SelectNodes("IncludeList/ContentPatterns/Pattern"));
            FilesSearchPaths = NormalizePathes(LoadValues(doc.DocumentElement.SelectNodes("FilesSearchPaths/Path")), fileDir);
            QuarantineOutputPath = NormalizePathes(LoadValues(doc.DocumentElement.SelectNodes("QuarantineOutputPath")), fileDir).FirstOrDefault();
            CodeSpecificationPath = NormalizePathes(LoadValues(doc.DocumentElement.SelectNodes("CodeSpecificationPath")), fileDir).FirstOrDefault();
            BackUpOutputPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CodeCleaner.Helpers.PathHelper.AppDataName), "BackUp");
        }

        private IList<string> LoadValues(XmlNodeList nodes)
        {
            List<string> result = new List<string>();

            foreach (XmlNode node in nodes)
            {
                result.Add(GetAttribute(node, "Value"));
            }

            return result;
        }

        private static IList<string> NormalizePathes(IList<string> pathes, string relativePath)
        {
            Checker.NotNull(pathes, "pathes");
            var result = new List<string>();
            string oldDir = Environment.CurrentDirectory;

            Environment.CurrentDirectory = relativePath;

            foreach (var path in pathes)
            {
                var info = new DirectoryInfo(path);
                result.Add(info.FullName);
            }

            Environment.CurrentDirectory = oldDir;

            return result;
        }

        #endregion

        #endregion

        #region ICodeCleanerProject

        public string BackUpOutputPath
        {
            get;
            private set;
        }

        public string CodeSpecificationPath
        {
            get;
            private set;
        }

        public IList<string> ExcludeContentPatterns
        {
            get;
            private set;
        }

        public IList<string> ExcludeFilenamePatterns
        {
            get;
            private set;
        }

        public IList<string> FilesSearchPaths
        {
            get;
            private set;
        }

        public IList<string> IncludeContentPatterns
        {
            get;
            private set;
        }

        public IList<string> IncludeFilenamePatterns
        {
            get;
            private set;
        }

        public string QuarantineOutputPath
        {
            get;
            private set;
        }

        #endregion
    }
}
