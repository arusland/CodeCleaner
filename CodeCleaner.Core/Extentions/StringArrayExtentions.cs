using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;

namespace CodeCleaner.Extentions
{
    public static class StringArrayExtentions
    {
        #region Methods
        
        #region Public

        public static bool CompareTo(this string[] This, string[] value)
        {
            Checker.NotNull(This);
            Checker.NotNull(value);

            if (This.Length == value.Length)
            {
                for (int i = 0; i < This.Length; i++)
                {
                    if (This[i] != value[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        #endregion

        #endregion
    }
}
