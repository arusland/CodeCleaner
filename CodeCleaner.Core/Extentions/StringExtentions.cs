using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;

namespace CodeCleaner.Extentions
{
    public static class StringExtentions
    {
        public static string UpFirstChar(this string value)
        {
            Checker.NotNullOrEmpty(value);

            return value[0].ToString().ToUpper() + value.Substring(1, value.Length - 1);
        }

        public static string TrimBraces(this string input)
        {
            if (input[0] == '{' && input[input.Length - 1] == '}')
            {
                return input.Substring(1, input.Length - 2);
            }

            throw new InvalidOperationException("Braces not found.");
        }

        public static string TrimBrackets(this string input)
        {
            if (input[0] == '[' && input[input.Length - 1] == ']')
            {
                return input.Substring(1, input.Length - 2);
            }

            throw new InvalidOperationException("Brackets not found.");
        }

        public static string TrimParenthesis(this string input)
        {
            if (input[0] == '(' && input[input.Length - 1] == ')')
            {
                return input.Substring(1, input.Length - 2);
            }

            throw new InvalidOperationException("Parenthesis not found.");
        }

        public static string[] SplitAndClear(this string input, char separator)
        {
            return input.Split(separator).Select(p => p.Trim()).Where(p => p.IsNotNullOrEmpty()).ToArray();
        }

        public static string[] SplitBySpaces(this string input)
        {
            return input.Split(new char[]{' ', '\n', '\r', '\t'}).Select(p => p.Trim()).Where(p => p.IsNotNullOrEmpty()).ToArray();
        }

        public static string[] ParseArguments(this string input)
        {
            MatchCollection mc = Regex.Matches(input, @"([^\,\<\>\[\]]+(\<.+?\>|\[.*?\])?\s*\w+\s*),?");
            List<string> result = new List<string>();
            
            foreach (Match m in mc)
            {
                result.Add(m.Groups[1].Value.Trim());
            }

            return result.ToArray();
        }

        public static bool IdenticalTo(this string input, string value)
        {
            const string PATTERN_Space = @"\s+";

            if (Regex.IsMatch(input, PATTERN_Space) && Regex.IsMatch(value, PATTERN_Space))
            {
                input = Regex.Replace(input, PATTERN_Space, string.Empty);
                value = Regex.Replace(value, PATTERN_Space, string.Empty);
            }

            return input == value;
        }
    }
}
