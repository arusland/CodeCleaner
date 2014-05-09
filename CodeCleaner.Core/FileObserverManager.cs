using System;
using Orygin.Shared.Minimal.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CodeCleaner.Helpers;
using Orygin.Shared.Minimal.Helpers;

namespace CodeCleaner
{
    public class FileObserverManager : IFileObserverManager
    {
        #region Constants

        private const string DB_FileName = "files.hash";
        private const int MAX_RecordCount = 10000;

        #endregion

        #region Fields

        private readonly IList<string> _ActiveFiles;
        private readonly string _DbFileName;
        private readonly IDictionary<string, string> _FileHashes;
        private readonly string _Version;

        #endregion

        #region Ctors

        public FileObserverManager()
        {
            _Version = this.GetType().Assembly.GetName().Version.ToString();
            _ActiveFiles = new List<string>();
            _FileHashes = new Dictionary<string, string>();
            _DbFileName = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CodeCleaner.Helpers.PathHelper.AppDataName), DB_FileName);
            Load();
        }

        #endregion

        #region Methods

        #region Private

        private string ComposeTime(string file, string hash)
        {
            return Strings.Format("{0}|{1}", file, hash);
        }

        private void Load()
        {
            if (File.Exists(_DbFileName))
            {
                using (var sr = new StreamReader(_DbFileName))
                {
                    var line = sr.ReadLine();

                    if (line.IsNotNullOrEmpty())
                    {
                        if (_Version == line)
                        {
                            line = sr.ReadLine();

                            while (line.IsNotNullOrEmpty())
                            {
                                string hash;
                                var file = ParseItem(line, out hash);

                                _FileHashes[file] = hash;
                                line = sr.ReadLine();
                            }
                        }
                    }
                }
            }
        }

        private void MakeActive(string filePath)
        {
            if (!_ActiveFiles.Contains(filePath))
            {
                _ActiveFiles.Add(filePath);
            }
        }

        private string MakeHash(string file)
        {
            var fi = new FileInfo(file);

            if (fi.Exists)
            {
                return Strings.Format("{0}-{1}", fi.LastWriteTime.Ticks, fi.Length);
            }

            return "file_not_found." + Guid.NewGuid().ToString();
        }

        private string ParseItem(string line, out string hash)
        {
            var splitted = line.Split('|').Select(p => p.Trim()).ToArray();

            if (splitted.Length >= 2)
            {
                hash = splitted[1];
                return splitted[0];
            }

            hash = string.Empty;
            return string.Empty;
        }

        #endregion

        #endregion

        #region IFileObserverManager

        public bool IsChanged(string filePath)
        {
            bool result;

            filePath = filePath.ToLower();

            if (_FileHashes.ContainsKey(filePath))
            {
                var hash = MakeHash(filePath);

                result = _FileHashes[filePath] != hash;
            }
            else
            {
                result = true;
            }

            MakeActive(filePath);

            return result;
        }

        public void RemoveFile(string filePath)
        {
            filePath = filePath.ToLower();

            _FileHashes.Remove(filePath);
        }

        public void Save()
        {
            var fi = new FileInfo(_DbFileName);

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            using (var sw = new StreamWriter(_DbFileName))
            {
                sw.WriteLine(_Version);

                int count = 0;
                // save only items which were active
                foreach (var file in _FileHashes.Where(p => _ActiveFiles.Contains(p.Key)).OrderByDescending(p => p.Value))
                {
                    sw.WriteLine(ComposeTime(file.Key, file.Value));
                    count++;
                }

                // save the rest not active items
                foreach (var file in _FileHashes.Where(p => !_ActiveFiles.Contains(p.Key)).OrderByDescending(p => p.Value))
                {
                    if (++count <= MAX_RecordCount)
                    {
                        sw.WriteLine(ComposeTime(file.Key, file.Value));
                    }
                    else
                        break;
                }
            }
        }

        public void SetFile(string filePath)
        {
            filePath = filePath.ToLower();

            _FileHashes[filePath] = MakeHash(filePath);

            MakeActive(filePath);
        }

        #endregion
    }
}
