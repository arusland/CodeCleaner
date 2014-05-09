using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Resources;
using Orygin.Shared.Minimal.Helpers;

namespace CodeCleaner.Helpers
{
    internal static class ResourceHelper
    {

        #region Methods
        
        #region Internal

        internal static void AddLangSymbols(string fileName, IDictionary<string, string> newValues)
        {
            Checker.NotNullOrEmpty(fileName, "fileName");
            Checker.NotNull(newValues, "newValues");

            var existingList = new List<DictionaryEntry>();

            using (var rsxr = new ResXResourceReader(fileName))
            {
                foreach (DictionaryEntry value in rsxr)
                {
                    existingList.Add(value);
                }
            }

            using (var rsxw = new ResXResourceWriter(fileName))
            {
                foreach (var value in existingList)
                {
                    rsxw.AddResource(value.Key.ToString(), value.Value);
                }

                foreach (var value in newValues.Where(p => !existingList.Any(g => g.Key.Equals(p.Key))))
                {
                    Debug.WriteLine(string.Format("Adding string {0}({1}) to {2}", value.Key, value.Value, fileName));
                    rsxw.AddResource(value.Key, value.Value);
                }
            }
        }

        internal static IDictionary<string, string> GetLangSymbols(string fileName)
        {
            var result = new Dictionary<string, string>();

            using (var rsxr = new ResXResourceReader(fileName))
            {
                foreach (DictionaryEntry value in rsxr)
                {
                    result.Add(value.Key.ToString(), value.Value.ToString());
                }
            }

            return result;
        }
        
        #endregion
        
        #endregion
    }
}
