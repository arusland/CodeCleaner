using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orygin.Shared.Minimal.Extensions;
using CodeCleaner.CodeBlocks;
using CodeCleaner.Extentions;

namespace CodeCleaner
{
    delegate string ReplaceDelegate(string value);
    delegate CodeBlock ParseCallbackDelegate(string block, Match m, CodeBlock parent, int blockLineNummer);

    public class CodeParser : ICodeParser
    {
        #region Constants

        private const string TEMPLATE_WholePattern = "^{0}$";
        private const String REPLACE_STRING_TOKEN_Template = "T%O%K{0}EN";
        private const String REPLACE_BRACE_TOKEN_Template = "`BR{0}ACE`";
        private const String REPLACE_PARENTHESIS_TOKEN_Template = "'SKO{0}BKI'";
        private const String REPLACE_BRACKET_TOKEN_Template = "@BRAC{0}KET@";
        private const String REPLACE_REGION_TOKEN_Template = "`REG{0}ION`";
        private const String REPLACE_COMMENT_TOKEN_Template = "&COM{0}MENT&";
        private const String REPLACE_DIRECTIVE_TOKEN_Template = "&DIR{0}ECTIVE&";
        private const String REPLACE_USING_TOKEN_Template = "&USI{0}NG&";
        private string[] MODIFICATOR_Array = { "public", "internal", "protected", "private" };
        private string[] CLASS_TYPE_Array = { "partial", "abstract", "static", "sealed" };
        private const string KEY_Const = "const";
        private const string KEY_Static = "static";
        private const string KEY_Event = "event";
        private const string KEY_Readonly = "readonly";
        private const string KEY_Partial = "partial";
        private const string KEY_Virtual = "virtual";
        private const string KEY_Override = "override";
        private const string KEY_New = "new";
        private const string KEY_Delegate = "delegate";
        private const string KEY_Abstract = "abstract";
        private const string KEY_Sealed = "sealed";
        private const string KEY_Operator = "operator";
        private const string KEY_Implicit = "implicit";
        private const string KEY_DependencyProperty = "DependencyProperty";
        private const string KEY_RoutedEvent = "RoutedEvent";
        private const string PATTERN_Fields = @"^([^;`\=]+)=([^;]+);+|([^;`'\=:]+);+$";
        private const string PATTERN_Directives = @"#if\s+[^#\n]+\n*?|#elif\s+[^#\n]+\n*?|#endif\s*\n*?|#else\s*\n*?";
        private const string PATTERN_SingleLineDirectives = @"#pragma\s+[^#\n]+\n*?|#line\s+[^#\n]+\n*?";
        private const string CC_DIRECTIVE_SKIPPED = "// CODECLEANER: OFF";

        #endregion

        #region Fields

        private Dictionary<string, string> _ReplacesBraces;
        private Dictionary<string, string> _ReplacesUsings;
        private Dictionary<string, string> _ReplacesComments;
        private Dictionary<string, string> _ReplacesRegions;
        private Dictionary<string, string> _ReplacesParenthesis;
        private Dictionary<string, string> _ReplacesBrackets;
        private Dictionary<string, string> _ReplacesDirectives;
        private Dictionary<string, string> _Strings;
        private string _ParsingFilename;
        private string _ContentWithoutComments;
        private string _CurrentClassName;
        private readonly string _TemplateBrace;
        private readonly string _TemplateParentheses;
        private readonly string _TemplateRegion;
        private readonly string _TemplateConstructor;
        private readonly string _TemplateBracket;
        private readonly string _TemplateString;
        private readonly string _TemplateComment;
        private readonly string _TemplateDirective;
        private readonly string _TemplateUsing;
        private readonly string _TemplateWhere;

        #endregion

        #region Ctors

        public CodeParser()
        {
            _TemplateBrace = string.Format(REPLACE_BRACE_TOKEN_Template, @"\d+");
            _TemplateParentheses = string.Format(REPLACE_PARENTHESIS_TOKEN_Template, @"\d+");
            _TemplateRegion = string.Format(REPLACE_REGION_TOKEN_Template, @"\d+");
            _TemplateBracket = string.Format(REPLACE_BRACKET_TOKEN_Template, @"\d+");
            _TemplateString = string.Format(REPLACE_STRING_TOKEN_Template, @"\d+");
            _TemplateUsing = string.Format(REPLACE_USING_TOKEN_Template, @"\d+");
            _TemplateComment = string.Format(REPLACE_COMMENT_TOKEN_Template, @"\d+");
            _TemplateDirective = string.Format(REPLACE_DIRECTIVE_TOKEN_Template, @"\d+");
            _TemplateConstructor = string.Format(@"^([^;`]+)({1})\s*:([^`]+{1})\s*({0})$", _TemplateBrace, _TemplateParentheses);
            _TemplateWhere = @"where\s+\w+\s*:\s*\w+\s*\,*\s*\w*\s*";
        }

        #endregion

        #region Methods

        #region Private

        public static string RemoveSpaces(string input)
        {
            string result = Regex.Replace(input, @"\s+([^\w\s])|([^\w\s])\s+",
                (m) =>
                {
                    return m.Groups[1].Length > 0 ? m.Groups[1].Value : m.Groups[2].Value;
                });

            if (input != result)
            {
                return RemoveSpaces(result);
            }

            return result;
        }

        private static bool IsIdChar(char value)
        {
            return value >= 'A' && value <= 'Z' ||
                value >= 'a' && value <= 'z' ||
                value >= '0' && value <= '9' || value == '_';
        }

        private string ExtractExplicitName(string input, string inSearch, out string explicitInterfaceName, out string name)
        {
            int braceCount = 0;
            bool hasIdChar = false;
            inSearch = RemoveSpaces(inSearch);
            int index = inSearch.Length - 1;
            int firstDotIndex = -1;
            bool hasIdAfterDot = false;

            while (index >= 0)
            {
                char curChar = inSearch[index];

                if (curChar == '>')
                {
                    if (hasIdChar && firstDotIndex == -1)
                    {
                        explicitInterfaceName = string.Empty;
                        name = inSearch.Substring(index + 1);
                        return inSearch.Substring(0, index + 1);
                    }
                    else if (hasIdAfterDot)
                    {
                        explicitInterfaceName = inSearch.Substring(index + 1, firstDotIndex - index - 1);
                        name = inSearch.Substring(firstDotIndex + 1);
                        return inSearch.Substring(0, index + 1);
                    }
                    else
                        braceCount++;
                }
                else if (curChar == '<')
                {
                    if (braceCount >= 1)
                    {
                        braceCount--;
                    }
                    else
                    {
                        ThrowException(ExceptionCode.InvalidPropertyFound, GetLineNumberByContent(input));
                    }
                }
                else if (IsIdChar(curChar))
                {
                    if (braceCount <= 0)
                    {
                        if (firstDotIndex == -1)
                        {
                            hasIdChar = true;
                        }
                        else
                        {
                            hasIdAfterDot = true;
                        }
                    }
                }
                else 
                {
                    if (curChar == '.')
                    {
                        if (firstDotIndex == -1)
                        {
                            firstDotIndex = index;
                        }
                    }
                    else if (braceCount <= 0 && hasIdChar)
                    {
                        if (firstDotIndex > 0)
                        {
                            explicitInterfaceName = inSearch.Substring(index + 1, firstDotIndex - index - 1);
                            name = inSearch.Substring(firstDotIndex + 1);
                        }
                        else
                        {
                            explicitInterfaceName = string.Empty;
                            name = inSearch.Substring(index + 1);
                        }

                        return inSearch.Substring(0, index + 1);
                    }
                }

                index--;
            }

            ThrowException(ExceptionCode.InvalidPropertyFound, GetLineNumberByContent(input));
            throw new InvalidOperationException("This code unreachable!");            
        }

        private string ExtractIndexProperty(string input, out string explicitInterfaceName)
        {
            string dummyName;

            return ExtractExplicitName(input, input, out explicitInterfaceName, out dummyName);
        }

        //private static string[] CheckAttributes(string attributes)
        //{
        //    string[] result = new string[0];
        //    attributes = attributes.Trim();

        //    if (attributes.IsNotEmpty())
        //    {
        //        MatchCollection mc = Regex.Matches(attributes, @"\[([^`\[\]]+?)\]");

        //        result = mc.OfType<Match>().Select(p => p.Value).ToArray();
        //    }

        //    return result;
        //}

        private string ExtractFieldAttributes(string input, out CodeBlock[] preBlocks, CodeBlock parent)
        {
            MatchCollection mc = Regex.Matches(input, @"\[([^`\[\]]+?)\]");
            int index = 0;     
            List<CodeBlock> blocksResult = new List<CodeBlock>();

            foreach (Match m in mc)
            {
                string preContent = TrimCode(input.Substring(index, m.Index - index));

                if (preContent.IsNotEmpty())
                {
                    if (Regex.IsMatch(preContent, _TemplateComment))
                    {
                        blocksResult.AddRange(ParseBlockContent(preContent, parent));
                    }
                    else
                    {
                        break;
                    }
                }

                blocksResult.Add(new CodeBlockAttribute(m.Value, GetLineNumberByContent(input), parent));
                index += m.Index - index + m.Length;
            }

            preBlocks = blocksResult.ToArray();

            return input.Substring(index);
        }

        private static string ReplaceRegExAll(string input, string pattern, string replaceBy, ReplaceDelegate delegat)
        {
            MatchCollection mc = Regex.Matches(input, pattern);
            string sRet = input;

            if (mc.Count > 0)
            {
                List<string> ht = new List<string>();

                for (int i = 0; i < mc.Count; i++)
                {
                    if (!ht.Contains(mc[i].Value))
                        ht.Add(mc[i].Value);
                }

                string[] sa = ht.ToArray();

                Array.Sort(sa, CompareStringsByLength);

                for (int i = 0; i < sa.Length; i++)
                {
                    if (delegat.IsNull())
                    {
                        sRet = sRet.Replace(sa[i], replaceBy);
                    }
                    else
                    {
                        sRet = sRet.Replace(sa[i], delegat(sa[i]));
                    }
                }
            }

            return sRet;
        }

        private static string RemovePatternInternal(string input, string pattern, ref int tokenCounter,
            ref Dictionary<string, string> replaces, string replaceTemplate, ReplaceDelegate replaceHandler)
        {
            MatchCollection mc = Regex.Matches(input, pattern);
            List<String> ht = new List<string>();

            for (int i = 0; i < mc.Count; i++)
            {
                if (!ht.Contains(mc[i].Value))
                    ht.Add(mc[i].Value);
            }

            String[] sa = ht.ToArray();
            Array.Sort(sa, CompareStringsByLength);

            for (int i = 0; i < sa.Length; i++)
            {
                String token = String.Format(replaceTemplate, tokenCounter++);
                input = input.Replace(sa[i], token);

                if (replaceHandler.IsNotNull())
                {
                    replaces.Add(token, replaceHandler(sa[i]));
                }
                else
                {
                    replaces.Add(token, sa[i]);
                }
            }

            return input;
        }

        private static Dictionary<string, string> GetAllStrings(ref String input)
        {
            Dictionary<string, string> replaces = new Dictionary<string, string>();
            int tokenCounter = 0;

            input = RemovePatternInternal(input, "@\"[^\\n]*?\"", ref tokenCounter, ref replaces, REPLACE_STRING_TOKEN_Template, null);
            input = RemovePatternInternal(input, "\"[^\\n]*?[^\\\\]\"", ref tokenCounter, ref replaces, REPLACE_STRING_TOKEN_Template, null);
            input = RemovePatternInternal(input, @"'[^']?[^\\]'|'\\\\'", ref tokenCounter, ref replaces, REPLACE_STRING_TOKEN_Template, null);

            return replaces;
        }

        private static int CompareStringsByLength(string str1, string str2)
        {
            if (str1.Length > str2.Length)
            {
                return -1;
            }
            else if (str1.Length < str2.Length)
            {
                return 1;
            }

            return 0;
        }

        private static string CallbackReplace(string value)
        {
            MatchCollection mc = Regex.Matches(value, @"\n");
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < mc.Count; i++)
            {
                result.Append(Environment.NewLine);
            }

            return result.ToString();
        }

        private string RemoveDirectives(string input)
        {
            int counter = 0;
            input = RemovePattern(input, PATTERN_SingleLineDirectives, ref counter, ref _ReplacesDirectives, REPLACE_DIRECTIVE_TOKEN_Template, null);

            input = ReplaceRegExAll(input, PATTERN_Directives, Environment.NewLine, null);
            return input;
        }

        private static string RemovePattern(string input, string pattern, ref int counter, ref Dictionary<string, string> replaces, string replaceTemplate, ReplaceDelegate replaceHandler)
        {
            string result = RemovePatternInternal(input, pattern, ref counter, ref replaces, replaceTemplate, replaceHandler);

            if (result != input)
            {
                return RemovePattern(result, pattern, ref counter, ref replaces, replaceTemplate, replaceHandler);
            }
            else
            {
                return result;
            }
        }

        private string RemoveComments(string input)
        {
            int counter = 0;

            string result = RemovePattern(input, @"(//[^\n]+[\n\s\t]*)+", ref counter, ref _ReplacesComments, REPLACE_COMMENT_TOKEN_Template, null);
            result = RemovePattern(result, @"/\*[^\0]*?\*/", ref counter, ref _ReplacesComments, REPLACE_COMMENT_TOKEN_Template, null);

            return result;
        }

        private string RemoveUsings(string input)
        {
            int counter = 0;
            return RemovePattern(input, @"using\s+[\w\.\s=]+;+", ref counter, ref _ReplacesUsings, REPLACE_USING_TOKEN_Template, null);
        }

        private static string RemoveBraces(string input, ref int counter, ref Dictionary<string, string> replaces)
        {
            return RemovePattern(input, @"\{[^\}\{#\(\)\[\]]*?\}", ref counter, ref replaces, REPLACE_BRACE_TOKEN_Template, null);
        }

        private static string RemoveBrackets(string input, ref int counter, ref Dictionary<string, string> replaces)
        {
            return RemovePattern(input, @"\[[^\}\{#\(\)\[\]]*?\]", ref counter, ref replaces, REPLACE_BRACKET_TOKEN_Template, null);
        }

        private static string RemoveParenthesis(string input, ref int counter, ref Dictionary<string, string> replaces)
        {
            return RemovePattern(input, @"\([^\}\{#\(\)\[\]]*?\)", ref counter, ref replaces, REPLACE_PARENTHESIS_TOKEN_Template, null);
        }

        private static string RemoveRegions(string input, ref int counter, ref Dictionary<string, string> replaces)
        {
            return RemovePattern(input, @"#region[^\{\}#\(\)\[\]]*?#endregion[^\r\n]*", ref counter, ref replaces, REPLACE_REGION_TOKEN_Template, null);
        }

        private string RemoveBracesAndRegions(string input)
        {
            int braceCounter = 0;
            int regionCounter = 0;
            int parenthesisCounter = 0;
            int bracketCounter = 0;

            while (true)
            {
                string result = RemoveBraces(input, ref braceCounter, ref _ReplacesBraces);

                result = RemoveRegions(result, ref regionCounter, ref _ReplacesRegions);

                result = RemoveParenthesis(result, ref parenthesisCounter, ref _ReplacesParenthesis);

                result = RemoveBrackets(result, ref bracketCounter, ref _ReplacesBrackets);

                if (result != input)
                {
                    input = result;
                }
                else
                {
                    return result;
                }
            }

            throw new InvalidOperationException("");
        }

        private string ExtractFromPackedCode1(string input)
        {
            if (input.Length <= 0)
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(input);
            string resultStr = input;

            do
            {
                input = result.ToString();

                if (Regex.IsMatch(resultStr, _TemplateBrace))
                {
                    foreach (string key in _ReplacesBraces.Keys)
                    {
                        result.Replace(key, _ReplacesBraces[key]);
                    }

                    resultStr = result.ToString();
                }

                if (Regex.IsMatch(resultStr, _TemplateRegion))
                {
                    foreach (string key in _ReplacesRegions.Keys)
                    {
                        result.Replace(key, _ReplacesRegions[key]);
                    }

                    resultStr = result.ToString();
                }

                if (Regex.IsMatch(resultStr, _TemplateParentheses))
                {
                    foreach (string key in _ReplacesParenthesis.Keys)
                    {
                        result.Replace(key, _ReplacesParenthesis[key]);
                    }

                    resultStr = result.ToString();
                }

                if (Regex.IsMatch(resultStr, _TemplateBracket))
                {
                    foreach (string key in _ReplacesBrackets.Keys)
                    {
                        result.Replace(key, _ReplacesBrackets[key]);
                    }

                    resultStr = result.ToString();
                }

            } while (input != resultStr);

            if (Regex.IsMatch(resultStr, _TemplateString))
            {
                foreach (string key in _Strings.Keys)
                {
                    result.Replace(key, _Strings[key]);
                }
            }

            return result.ToString();
        }

        private string ExtractFromPackedCode(string input)
        {
            if (input.Length <= 0)
            {
                return string.Empty;
            }

            string result = input;

            do
            {
                input = result;

                if (Regex.IsMatch(result, _TemplateBrace))
                {
                    foreach (string key in _ReplacesBraces.Keys)
                    {
                        if (result.Contains(key))
                        {
                            result = result.Replace(key, _ReplacesBraces[key]);
                        }
                    }
                }

                if (Regex.IsMatch(result, _TemplateRegion))
                {
                    foreach (string key in _ReplacesRegions.Keys)
                    {
                        if (result.Contains(key))
                        {
                            result = result.Replace(key, _ReplacesRegions[key]);
                        }
                    }
                }

                if (Regex.IsMatch(result, _TemplateParentheses))
                {
                    foreach (string key in _ReplacesParenthesis.Keys)
                    {
                        if (result.Contains(key))
                        {
                            result = result.Replace(key, _ReplacesParenthesis[key]);
                        }
                    }
                }

                if (Regex.IsMatch(result, _TemplateBracket))
                {
                    foreach (string key in _ReplacesBrackets.Keys)
                    {
                        if (result.Contains(key))
                        {
                            result = result.Replace(key, _ReplacesBrackets[key]);
                        }
                    }
                }

                if (Regex.IsMatch(result, _TemplateUsing))
                {
                    foreach (string key in _ReplacesUsings.Keys)
                    {
                        if (result.Contains(key))
                        {
                            result = result.Replace(key, _ReplacesUsings[key]);
                        }
                    }
                }

                if (Regex.IsMatch(result, _TemplateComment))
                {
                    foreach (string key in _ReplacesComments.Keys)
                    {
                        if (result.Contains(key))
                        {
                            result = result.Replace(key, _ReplacesComments[key]);
                        }
                    }
                }


                if (Regex.IsMatch(result, _TemplateDirective))
                {
                    foreach (string key in _ReplacesDirectives.Keys)
                    {
                        if (result.Contains(key))
                        {
                            result = result.Replace(key, _ReplacesDirectives[key]);
                        }
                    }
                }

            } while (input != result);

            if (Regex.IsMatch(result, _TemplateString))
            {
                foreach (string key in _Strings.Keys)
                {
                    if (result.Contains(key))
                    {
                        result = result.Replace(key, _Strings[key]);
                    }
                }
            }

            return result;
        }

        private int GetLineNumber(string input, int index)
        {
            if (index > 0)
            {
                string substring = ExtractFromPackedCode(input.Substring(0, index + 1));
                MatchCollection mc = Regex.Matches(substring, @"\n");

                return mc.Count + 1;
            }
            else
            {
                return 1;
            }
        }

        private string GetRegionContent(string content, out string name)
        {
            Match m = Regex.Match(content, @"#region([^\n\r]*)([^\{\}#]*?)#endregion");

            name = ExtractFromPackedCode(m.Groups[1].Value).Trim();
            return m.Groups[2].Value;
        }

        private ModificatorType CheckModificatorType(string value)
        {
            string modificator = value.Trim();
            ModificatorType accessMod = modificator.IsNotEmpty() ? modificator.UpFirstChar().ParseEnum<ModificatorType>() : ModificatorType.Default;

            return accessMod;
        }

        private string CheckClassName(string value)
        {
            return value.Trim();
        }

        private string[] CheckInheritList(string value)
        {
            string[] splited = value.SplitAndClear(',');

            return splited;
        }

        private CodeBlock ParseFieldBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            bool useFirst = m.Groups[1].Length > 0;
            string leftSide = useFirst ? m.Groups[1].Value : m.Groups[3].Value;
            string rightSide = useFirst ? m.Groups[2].Value : string.Empty;
            CodeBlock[] attributes;
            string explicitInterfaceName;
            string fieldName;

            leftSide = ExtractFieldAttributes(ExtractFromPackedCode(leftSide), out attributes, parent);
            leftSide = ExtractEventParts(leftSide, out fieldName, out explicitInterfaceName);
            string header = GetHeaderAfter(block, m, useFirst ? 2 : 3);
            string[] splited = leftSide.SplitBySpaces();

            if (rightSide.IsNotEmpty())
            {
                rightSide = ExtractFromPackedCode(rightSide);
            }

            ModificatorType modificatorType = ModificatorType.Default;
            bool isConst = false;
            bool isStatic = false;
            bool isEvent = false;
            bool isReadonly = false;
            bool isDependencyProperty = false;
            bool isRoutedEvent = false;
            int indexUsed = -1;
            bool canContinue = true;

            for (int i = 0; i < splited.Length && canContinue; i++)
            {
                if (MODIFICATOR_Array.Any(p => p == splited[i]))
                {
                    indexUsed = i;
                    var modType = splited[i].UpFirstChar().ParseEnum<ModificatorType>();
                    modificatorType = ComposeModificators(modificatorType, modType);
                }
                else
                {
                    switch (splited[i])
                    {
                        case KEY_Const:
                            indexUsed = i;
                            isConst = true;
                            break;
                        case KEY_Event:
                            indexUsed = i;
                            isEvent = true;
                            break;
                        case KEY_Readonly:
                            indexUsed = i;
                            isReadonly = true;
                            break;
                        case KEY_Static:
                            indexUsed = i;
                            isStatic = true;
                            break;
                        case KEY_DependencyProperty:
                            indexUsed = i;
                            isDependencyProperty = true;
                            break;
                        case KEY_RoutedEvent:
                            indexUsed = i;
                            isRoutedEvent = true;
                            break;
                        default:
                            canContinue = false;
                            break;
                    }
                }
            }

            indexUsed++;

            int remainCount = splited.Length - indexUsed;

            if (remainCount < 1 && !(isDependencyProperty || isRoutedEvent))
            {
                // field without type
                ThrowException(ExceptionCode.InvalidFieldFound, blockLineNum);
            }

            StringBuilder typeBuilder = new StringBuilder();

            if (!isDependencyProperty)
            {
                for (int i = indexUsed; i < splited.Length; i++)
                {
                    if (typeBuilder.Length > 0)
                    {
                        typeBuilder.Append(' ');
                    }
                    typeBuilder.Append(splited[i]);
                }
            }

            CodeBlockType type = isEvent ? CodeBlockType.Event : (isConst ? CodeBlockType.Const :
                (isDependencyProperty ? CodeBlockType.DependencyProperty : (isRoutedEvent ? CodeBlockType.RoutedEvent : CodeBlockType.Field)));

            if (isEvent)
            {
                return new CodeBlockEvent(fieldName, modificatorType, typeBuilder.ToString(), isStatic, string.Empty, header,
                    attributes, explicitInterfaceName, blockLineNum, parent);
            }
            else if (isDependencyProperty)
            {
                return new CodeBlockDependencyProperty(fieldName, modificatorType, isStatic, rightSide, header, attributes, isReadonly, blockLineNum, parent);
            }
            else if (isRoutedEvent)
            {
                return new CodeBlockRoutedEvent(fieldName, modificatorType, isStatic, rightSide, header, attributes, isReadonly, blockLineNum, parent);
            }

            return new CodeBlockField(fieldName, type, modificatorType, typeBuilder.ToString(), rightSide, isStatic, isReadonly,
                ExtractFromPackedCode(m.Value), header, attributes, blockLineNum, parent);
        }

        private string ExtractPropertyParts(string input, out string propertyName, out string explicitInterfaceName)
        {
            string dummy;

            return ExtractExplicitParts(input, out propertyName, out dummy, out explicitInterfaceName);
        }

        private string ExtractEventParts(string input, out string eventName, out string explicitInterfaceName)
        {
            string dummy;

            return ExtractExplicitParts(input, out eventName, out dummy, out explicitInterfaceName);
        }

        private string ExtractConstructorParts(string input, out string constructorName, out string genericPart)
        {
            string dummy;

            return ExtractExplicitParts(input, out constructorName, out genericPart, out dummy);
        }

        private string IsMethodOperator(string methodName, out bool isOperator)
        {
            Match m = Regex.Match(methodName, @"^operator([^\w]+)$");

            if (m.Success)
            {
                isOperator = true;

                return m.Groups[1].Value.Trim();
            }

            isOperator = false;

            return methodName;
        }

        private string ExtractExplicitParts(string input, out string methodName, out string genericPart, out string explicitInterfaceName)
        {
            string result = ExtractExplicitName(input, input, out explicitInterfaceName, out methodName);

            methodName = ExtractGenericPart(methodName, out genericPart);

            return result;
        }

        private string GetHeaderAfter(string block, Match m, int index)
        {
            string result = ExtractFromPackedCode(block.Substring(m.Index, m.Groups[index].Index + m.Groups[index].Length - m.Index)).Trim();

            return result;
        }

        private string GetHeaderBefore(string block, Match m, int index)
        {
            string result = ExtractFromPackedCode(block.Substring(m.Index, m.Groups[index].Index - m.Index)).Trim();

            return result;
        }

        private static ModificatorType ComposeModificators(ModificatorType prev, ModificatorType current)
        {
            ModificatorType result;

            if (current == ModificatorType.Internal && prev == ModificatorType.Protected ||
                        current == ModificatorType.Protected && prev == ModificatorType.Internal)
            {
                result = ModificatorType.Internalprotected;
            }
            else
            {
                result = current;
            }

            return result;
        }

        private CodeBlock ParseMethodBasedBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            string typeAndMod;
            string methodName;
            string genericPart = string.Empty;
            string explicitInterfaceName;
            string[] arguments;
            bool isAbstract = false;
            string methodBody = string.Empty;
            CodeBlock[] attributes;
            bool isOperator = false;
            bool isImplicit = false;
            string header;

            if (m.Groups[1].Length > 0)
            {
                // abstract method
                isAbstract = true;
                typeAndMod = ExtractFromPackedCode(m.Groups[1].Value);
                arguments = ExtractFromPackedCode(m.Groups[2].Value).TrimParenthesis().ParseArguments();
                header = GetHeaderAfter(block, m, m.Groups[3].Length > 0 ? 3 : 2);
            }
            else
            {
                // implemented method
                typeAndMod = ExtractFromPackedCode(m.Groups[4].Value);
                arguments = ExtractFromPackedCode(m.Groups[5].Value).TrimParenthesis().ParseArguments();
                methodBody = ExtractFromPackedCode(_ReplacesBraces[m.Groups[7].Value].TrimBraces());
                header = GetHeaderAfter(block, m, m.Groups[6].Length > 0 ? 6 : 5);
            }

            typeAndMod = ExtractFieldAttributes(typeAndMod, out attributes, parent);
            typeAndMod = ExtractExplicitParts(typeAndMod, out methodName, out genericPart, out explicitInterfaceName);
            methodName = IsMethodOperator(methodName, out isOperator);
            string[] splited = typeAndMod.SplitBySpaces();

            ModificatorType modificatorType = ModificatorType.Default;
            int indexUsed = -1;
            bool canContinue = true;
            bool isStatic = false;
            bool isPartial = false;
            bool isNew = false;
            bool isVirtual = false;
            bool isOverride = false;
            bool isDelegate = false;

            for (int i = 0; i < splited.Length && canContinue; i++)
            {
                if (MODIFICATOR_Array.Any(p => p == splited[i]))
                {
                    indexUsed = i;
                    var modType = splited[i].UpFirstChar().ParseEnum<ModificatorType>();
                    modificatorType = ComposeModificators(modificatorType, modType);
                }
                else
                {
                    switch (splited[i])
                    {
                        case KEY_Static:
                            indexUsed = i;
                            isStatic = true;
                            break;
                        case KEY_New:
                            indexUsed = i;
                            isNew = true;
                            break;
                        case KEY_Override:
                            indexUsed = i;
                            isOverride = true;
                            break;
                        case KEY_Virtual:
                            indexUsed = i;
                            isVirtual = true;
                            break;
                        case KEY_Abstract:
                            indexUsed = i;
                            isAbstract = true;
                            break;
                        case KEY_Delegate:
                            indexUsed = i;
                            isDelegate = true;
                            break;
                        case KEY_Partial:
                            indexUsed = i;
                            isPartial = true;
                            break;
                        case KEY_Operator:
                            indexUsed = i;
                            isOperator = true;
                            break;
                        case KEY_Implicit:
                            indexUsed = i;
                            isImplicit = true;
                            break;
                        default:
                            canContinue = false;
                            break;
                    }
                }
            }

            indexUsed++;

            int remainCount = splited.Length - indexUsed;
            StringBuilder typeBuilder = new StringBuilder();
            bool isConstructor = false, isDestructor = false;

            if (remainCount > 0)
            {
                if (remainCount == 1 && splited[0].Equals("~"))
                {
                    isDestructor = true;
                }
                else
                {
                    for (int i = indexUsed; i < splited.Length; i++)
                    {
                        if (typeBuilder.Length > 0)
                        {
                            typeBuilder.Append(' ');
                        }
                        typeBuilder.Append(splited[i]);
                    }
                }
            }
            else
            {
                isConstructor = true;
            }

            if (isConstructor)
            {
                if (methodName != _CurrentClassName)
                {
                    ThrowException(ExceptionCode.InvalidConstructorFound, blockLineNum);
                }

                return new CodeBlockConstructor(methodName, modificatorType, isStatic, arguments, string.Empty, methodBody, header, genericPart,
                    attributes, blockLineNum, parent);
            }
            else if (isDestructor)
            {
                if (methodName != _CurrentClassName)
                {
                    ThrowException(ExceptionCode.InvalidDestructorFound, blockLineNum);
                }

                return new CodeBlockDestructor(methodName, modificatorType, arguments, methodBody, header, 
                    attributes, blockLineNum, parent);
            }
            else if (isDelegate)
            {
                return new CodeBlockDelegate(methodName, modificatorType, typeBuilder.ToString(), arguments, genericPart,
                    attributes, blockLineNum, header, parent);
            }
            else if (isOperator)
            {
                if (!isStatic)
                {
                    ThrowException(ExceptionCode.InvalidOperatorFound, blockLineNum);
                }

                return new CodeBlockOperator(methodName, modificatorType, isImplicit, typeBuilder.ToString(), arguments, methodBody, header,
                    attributes, blockLineNum, parent);
            }

            return new CodeBlockMethod(methodName, modificatorType, isAbstract, isStatic, isVirtual, isOverride, isNew, isPartial,
                typeBuilder.ToString(), arguments, methodBody, header, genericPart, attributes, explicitInterfaceName, blockLineNum, parent);
        }

        private IList<CodeBlock> ParseBlocks(MatchCollection mc, string content, ParseCallbackDelegate callback, CodeBlock parent)
        {
            List<CodeBlock> result = new List<CodeBlock>();
            int index = 0;

            foreach (Match m in mc)
            {
                string preFieldContent = TrimCode(content.Substring(index, m.Index - index));

                if (preFieldContent.IsNotEmpty())
                {
                    result.AddRange(ParseBlockContent(preFieldContent, parent));
                }

                result.Add(DispatchInnerBlocks(callback(content, m, parent, GetLineNumberByContent(m.Value))));
                index += m.Index - index + m.Length;
            }

            string postClassContent = TrimCode(content.Substring(index));

            if (postClassContent.IsNotEmpty())
            {
                result.AddRange(ParseBlockContent(postClassContent, parent));
            }

            return result;
        }

        private CodeBlock ParseConstructorBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            string typeAndMod = ExtractFromPackedCode(m.Groups[1].Value.Trim());
            string constructorName;
            string[] arguments = ExtractFromPackedCode(m.Groups[2].Value).TrimParenthesis().ParseArguments();
            string additionalCallName = ExtractFromPackedCode(m.Groups[3].Value);
            string methodBody = ExtractFromPackedCode(_ReplacesBraces[m.Groups[4].Value].TrimBraces());
            ModificatorType modificatorType = ModificatorType.Default;
            int indexUsed = -1;
            bool canContinue = true;
            bool isStatic = false;
            string genericPart = string.Empty;
            CodeBlock[] attributes;
            string header = GetHeaderBefore(block, m, 4);

            typeAndMod = ExtractFieldAttributes(typeAndMod, out attributes, parent);
            typeAndMod = ExtractConstructorParts(typeAndMod, out constructorName, out genericPart);

            string[] splited = typeAndMod.SplitBySpaces();

            for (int i = 0; i < splited.Length && canContinue; i++)
            {
                if (MODIFICATOR_Array.Any(p => p == splited[i]))
                {
                    indexUsed = i;
                    var modType = splited[i].UpFirstChar().ParseEnum<ModificatorType>();
                    modificatorType = ComposeModificators(modificatorType, modType);
                }
                else
                {
                    switch (splited[i])
                    {
                        case KEY_Static:
                            indexUsed = i;
                            isStatic = true;
                            break;
                        default:
                            canContinue = false;
                            break;
                    }
                }
            }

            indexUsed++;

            int remainCount = splited.Length - indexUsed;

            if (constructorName != _CurrentClassName || remainCount > 0)
            {
                ThrowException(ExceptionCode.InvalidConstructorFound, blockLineNum);
            }

            return new CodeBlockConstructor(constructorName, modificatorType, isStatic, arguments, additionalCallName,
                methodBody, header, genericPart, attributes, blockLineNum, parent);
        }

        private CodeBlock ParsePropertyBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            string typeAndMod;
            CodeBlock[] attributes;
            string body;
            string[] arguments = null;
            bool isIndexProperty = false;
            string explicitInterfaceName = string.Empty;
            string propertyName = string.Empty;
            string header;

            if (m.Groups[1].Length > 0)
            {
                // is normal property
                typeAndMod = ExtractFieldAttributes(ExtractFromPackedCode(m.Groups[1].Value), out attributes, parent);
                body = ExtractFromPackedCode(_ReplacesBraces[m.Groups[2].Value].TrimBraces());
                typeAndMod = ExtractPropertyParts(typeAndMod, out propertyName, out explicitInterfaceName);
                header = GetHeaderBefore(block, m, 2);
            }
            else
            {
                // is index property
                typeAndMod = ExtractFieldAttributes(ExtractFromPackedCode(m.Groups[3].Value), out attributes, parent);
                isIndexProperty = true;
                arguments = ExtractFromPackedCode(m.Groups[4].Value).TrimBrackets().ParseArguments();
                body = ExtractFromPackedCode(_ReplacesBraces[m.Groups[5].Value].TrimBraces());
                typeAndMod = ExtractIndexProperty(typeAndMod, out explicitInterfaceName);
                header = GetHeaderBefore(block, m, 5);
            }            

            string[] splited = typeAndMod.SplitBySpaces();

            ModificatorType modificatorType = ModificatorType.Default;
            int indexUsed = -1;
            bool canContinue = true;
            bool isStatic = false;
            bool isNew = false;
            bool isVirtual = false;
            bool isOverride = false;
            bool isAbstract = false;
            bool isEvent = false;

            for (int i = 0; i < splited.Length && canContinue; i++)
            {
                if (MODIFICATOR_Array.Any(p => p == splited[i]))
                {
                    indexUsed = i;
                    var modType = splited[i].UpFirstChar().ParseEnum<ModificatorType>();
                    modificatorType = ComposeModificators(modificatorType, modType);
                }
                else
                {
                    switch (splited[i])
                    {
                        case KEY_Static:
                            indexUsed = i;
                            isStatic = true;
                            break;
                        case KEY_New:
                            indexUsed = i;
                            isNew = true;
                            break;
                        case KEY_Override:
                            indexUsed = i;
                            isOverride = true;
                            break;
                        case KEY_Abstract:
                            indexUsed = i;
                            isAbstract = true;
                            break;
                        case KEY_Virtual:
                            indexUsed = i;
                            isVirtual = true;
                            break;
                        case KEY_Event:
                            indexUsed = i;
                            isEvent = true;
                            break;
                        default:
                            canContinue = false;
                            break;
                    }
                }
            }

            indexUsed++;

            int remainCount = splited.Length - indexUsed;

            StringBuilder typeBuilder = new StringBuilder();

            if (remainCount > 0)
            {
                for (int i = indexUsed; i < splited.Length; i++)
                {
                    if (typeBuilder.Length > 0)
                    {
                        typeBuilder.Append(' ');
                    }
                    typeBuilder.Append(splited[i]);
                }
            }
            else
            {
                ThrowException(ExceptionCode.InvalidPropertyFound, blockLineNum);
            }

            // if it is index property
            if (isIndexProperty)
            {
                return new CodeBlockIndexProperty(modificatorType, typeBuilder.ToString(), isAbstract,
                    isVirtual, isOverride, isNew, body, header, arguments, attributes, explicitInterfaceName, blockLineNum, parent);
            }
            else if (isEvent)
            {
                return new CodeBlockEvent(propertyName, modificatorType, typeBuilder.ToString(), isStatic, body, header, attributes,
                    explicitInterfaceName, blockLineNum, parent);
            }

            return new CodeBlockProperty(propertyName, modificatorType, typeBuilder.ToString(), isAbstract,
                isStatic, isVirtual, isOverride, isNew, body, header, attributes, explicitInterfaceName, blockLineNum, parent);
        }

        private int GetLineNumberByContent(string content)
        {
            int index = _ContentWithoutComments.IndexOf(ExtractFromPackedCode(content).TrimStart());

            if (index >= 0)
            {
                MatchCollection mc = Regex.Matches(_ContentWithoutComments.Substring(0, index), @"\n");

                return mc.Count + 1;
            }

            return 0;
        }

        private CodeBlock ParseRegionBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            string regionGroup = _ReplacesRegions[m.Value];
            string regionName;
            string regionContent = GetRegionContent(regionGroup, out regionName);

            return new CodeBlockRegion(regionName, regionContent, blockLineNum, parent);
        }

        private CodeBlock ParseInterfaceBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            bool useFirst = m.Groups[3].Length > 0;
            CodeBlock[] attributes;
            string typeAndMod = useFirst ? m.Groups[2].Value : m.Groups[6].Value;
            string genericPart = string.Empty;
            string interfaceName = ExtractGenericPart(useFirst ? m.Groups[3].Value : m.Groups[7].Value, out genericPart);
            string header = GetHeaderBefore(block, m, useFirst ? 4 : 9);
            string body = _ReplacesBraces[useFirst ? m.Groups[4].Value : m.Groups[9].Value].TrimBraces();
            ExtractFieldAttributes(ExtractFromPackedCode(useFirst ? m.Groups[1].Value : m.Groups[5].Value), out attributes, parent);

            string[] splited = typeAndMod.SplitBySpaces();

            ModificatorType modificatorType = ModificatorType.Default;
            bool isPartial = false;
            bool canContinue = true;
            int indexUsed = -1;

            for (int i = 0; i < splited.Length && canContinue; i++)
            {
                if (MODIFICATOR_Array.Any(p => p == splited[i]))
                {
                    indexUsed = i;
                    var modType = splited[i].UpFirstChar().ParseEnum<ModificatorType>();
                    modificatorType = ComposeModificators(modificatorType, modType);
                }
                else
                {
                    switch (splited[i])
                    {
                        case KEY_Partial:
                            indexUsed = i;
                            isPartial = true;
                            break;
                        default:
                            canContinue = false;
                            break;
                    }
                }
            }

            indexUsed++;

            int remainCount = splited.Length - indexUsed;

            if (remainCount > 0)
            {
                ThrowException(ExceptionCode.InvalidInterfaceFound, blockLineNum);
            }

            return new CodeBlockInterface(interfaceName, modificatorType, CheckInheritList(useFirst ? string.Empty : m.Groups[8].Value),
                isPartial, body, header, genericPart, attributes, blockLineNum, parent);
        }

        private CodeBlock ParseEnumBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            string typeAndMod = m.Groups[2].Value;
            string enumName = m.Groups[3].Value;
            string body = ExtractFromPackedCode(_ReplacesBraces[m.Groups[5].Value].TrimBraces());
            string baseType = m.Groups[4].Value;
            string header = GetHeaderBefore(block, m, 5);
            CodeBlock[] attributes;
            string[] splited = typeAndMod.SplitBySpaces();
            ExtractFieldAttributes(ExtractFromPackedCode(m.Groups[1].Value), out attributes, parent);

            ModificatorType modificatorType = ModificatorType.Default;
            int indexUsed = -1;

            for (int i = 0; i < splited.Length; i++)
            {
                if (MODIFICATOR_Array.Any(p => p == splited[i]))
                {
                    indexUsed = i;
                    var modType = splited[i].UpFirstChar().ParseEnum<ModificatorType>();
                    modificatorType = ComposeModificators(modificatorType, modType);
                }
            }

            indexUsed++;

            int remainCount = splited.Length - indexUsed;

            if (remainCount > 0)
            {
                ThrowException(ExceptionCode.InvalidEnumFound, blockLineNum);
            }

            return new CodeBlockEnum(enumName, modificatorType, baseType, body, header, attributes, blockLineNum, parent);
        }

        private IList<CodeBlock> ParseStructs(MatchCollection mc, string content, CodeBlock parent)
        {
            List<CodeBlock> result = new List<CodeBlock>();
            int index = 0;
            string oldStructName = _CurrentClassName;

            foreach (Match m in mc)
            {
                string preStructContent = TrimCode(content.Substring(index, m.Index - index));
                if (preStructContent.IsNotEmpty())
                {
                    result.AddRange(ParseBlockContent(preStructContent, parent));
                }

                CodeBlock[] attributes;
                string typeAndMod = m.Groups[2].Value;
                string structName = m.Groups[3].Value;
                string body = _ReplacesBraces[m.Groups[4].Value].TrimBraces();
                string header = GetHeaderBefore(content, m, 4);
                int blockLineNum = GetLineNumberByContent(m.Value);

                ExtractFieldAttributes(ExtractFromPackedCode(m.Groups[1].Value), out attributes, parent);

                string[] splited = typeAndMod.SplitBySpaces();

                ModificatorType modificatorType = ModificatorType.Default;
                bool isPartial = false;
                bool canContinue = true;
                int indexUsed = -1;

                for (int i = 0; i < splited.Length && canContinue; i++)
                {
                    if (MODIFICATOR_Array.Any(p => p == splited[i]))
                    {
                        indexUsed = i;
                        var modType = splited[i].UpFirstChar().ParseEnum<ModificatorType>();
                        modificatorType = ComposeModificators(modificatorType, modType);
                    }
                    else
                    {
                        switch (splited[i])
                        {
                            case KEY_Partial:
                                indexUsed = i;
                                isPartial = true;
                                break;
                            default:
                                canContinue = false;
                                break;
                        }
                    }
                }

                indexUsed++;

                int remainCount = splited.Length - indexUsed;

                if (remainCount > 0)
                {
                    ThrowException(ExceptionCode.InvalidStructFound, GetLineNumberByContent(content));
                }

                _CurrentClassName = structName;
                result.Add(DispatchInnerBlocks(new CodeBlockStruct(structName, modificatorType, isPartial, body, header, attributes, 0, parent)));
                index += m.Index - index + m.Value.Length;
            }

            _CurrentClassName = oldStructName;
            string postStructContent = TrimCode(content.Substring(index));

            if (postStructContent.IsNotEmpty())
            {
                result.AddRange(ParseBlockContent(postStructContent, parent));
            }

            return result;
        }

        private IList<CodeBlock> ParseClasses(MatchCollection mc, string content, CodeBlock parent)
        {
            List<CodeBlock> result = new List<CodeBlock>();
            int index = 0;
            string oldClassName = _CurrentClassName;

            foreach (Match m in mc)
            {
                bool useFirst = m.Groups[3].Length > 0;
                string className = CheckClassName(useFirst ? m.Groups[3].Value : m.Groups[7].Value);
                string header = GetHeaderBefore(content, m, useFirst ? 4 : 9);
                string genericPart = string.Empty;
                CodeBlock[] attributes;                
                className = ExtractGenericPart(className, out genericPart);
                ExtractFieldAttributes(ExtractFromPackedCode(useFirst ? m.Groups[1].Value : m.Groups[5].Value), out attributes, parent);

                string preClassContent = TrimCode(content.Substring(index, m.Index - index));

                if (preClassContent.IsNotEmpty())
                {
                    result.AddRange(ParseBlockContent(preClassContent, parent));
                }

                // we assign className again because we may have nested classes
                string classBody = _ReplacesBraces[useFirst ? m.Groups[4].Value : m.Groups[9].Value].TrimBraces();
                string[] splited = ExtractFromPackedCode(useFirst ? m.Groups[2].Value : m.Groups[6].Value).SplitBySpaces();
                ModificatorType modificatorType = ModificatorType.Default;
                bool isStatic = false, isPartial = false,
                    isAbstract = false, isSealed = false;
                bool canContinue = true;
                int indexUsed = -1;

                for (int i = 0; i < splited.Length && canContinue; i++)
                {
                    if (MODIFICATOR_Array.Any(p => p == splited[i]))
                    {
                        indexUsed = i;
                        var modType = splited[i].UpFirstChar().ParseEnum<ModificatorType>();
                        modificatorType = ComposeModificators(modificatorType, modType);
                    }
                    else
                    {
                        switch (splited[i])
                        {
                            case KEY_Partial:
                                indexUsed = i;
                                isPartial = true;
                                break;
                            case KEY_Static:
                                indexUsed = i;
                                isStatic = true;
                                break;
                            case KEY_Abstract:
                                indexUsed = i;
                                isAbstract = true;
                                break;
                            case KEY_Sealed:
                                indexUsed = i;
                                isSealed = true;
                                break;
                            default:
                                canContinue = false;
                                break;
                        }
                    }
                }

                indexUsed++;

                int remainCount = splited.Length - indexUsed;

                if (remainCount > 0)
                {
                    ThrowException(ExceptionCode.InvalidClassFound, GetLineNumberByContent(content));
                }

                _CurrentClassName = className;
                result.Add(DispatchInnerBlocks(new CodeBlockClass(className, modificatorType,
                    CheckInheritList(useFirst ? string.Empty : m.Groups[8].Value), isPartial, isStatic,
                    isAbstract, isSealed, classBody, header, genericPart, attributes, 0, parent)));
                index += m.Index - index + m.Value.Length;
            }

            _CurrentClassName = oldClassName;
            string postClassContent = TrimCode(content.Substring(index));

            if (postClassContent.IsNotEmpty())
            {
                result.AddRange(ParseBlockContent(postClassContent, parent));
            }

            return result;
        }

        private string ExtractGenericPart(string name, out string genericPart)
        {
            int genericIndex = name.IndexOf('<');

            if (genericIndex >= 0)
            {
                genericPart = name.Substring(genericIndex);
                name = name.Remove(genericIndex);
            }
            else
            {
                genericPart = string.Empty;
            }

            return name.Trim();
        }

        private CodeBlock ParseCommentBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            return new CodeBlockComment(ExtractFromPackedCode(m.Value), blockLineNum, parent);
        }

        private CodeBlock ParseDirectiveBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            return new CodeBlockDirective(ExtractFromPackedCode(m.Value), blockLineNum, parent);
        }

        private CodeBlock ParseUsingBlock(string block, Match m, CodeBlock parent, int blockLineNum)
        {
            return new CodeBlockUsing(ExtractFromPackedCode(m.Value).TrimEnd(';').TrimEnd(), blockLineNum, parent);
        }

        private static string[] SplitByPattern(string input, string pattern)
        {
            var result = new List<string>();
            var matches = Regex.Matches(input, pattern);
            int start = 0;

            foreach (Match m in matches)
            {
                int length = m.Index - start + m.Length;
                var substr = input.Substring(start, length).Trim();
                result.Add(substr);
                start += length;
            }

            return result.ToArray();
        }

        private IList<CodeBlock> ParseBlockContent(string content, CodeBlock parent)
        {
            return ParseBlockContent(content, parent, false);
        }

        private IList<CodeBlock> ParseBlockContent(string content, CodeBlock parent, bool isSingleToken)
        {
            List<CodeBlock> result = new List<CodeBlock>();

            if (content.IsEmpty())
            {
                return result;
            }

            MatchCollection mc = Regex.Matches(content, _TemplateRegion);

            if (mc.Count > 0)
            {
                // handle regions
                result.AddRange(ParseBlocks(mc, content, ParseRegionBlock, parent));
                return result;
            }

            mc = Regex.Matches(content, _TemplateComment);

            if (mc.Count > 0)
            {
                // handle comments
                result.AddRange(ParseBlocks(mc, content, ParseCommentBlock, parent));
                return result;
            }

            mc = Regex.Matches(content, _TemplateDirective);

            if (mc.Count > 0)
            {
                // handle single-line directives
                result.AddRange(ParseBlocks(mc, content, ParseDirectiveBlock, parent));
                return result;
            }

            mc = Regex.Matches(content, _TemplateUsing);

            if (mc.Count > 0)
            {
                // handle usings
                result.AddRange(ParseBlocks(mc, content, ParseUsingBlock, parent));
                return result;
            }

            if (!isSingleToken)
            {
                var splitted = SplitByPattern(content, string.Format("{0};*|;+", _TemplateBrace));

                if (splitted.Length > 1)
                {
                    foreach (var block in splitted)
                    {
                        result.AddRange(ParseBlockContent(block, parent, true));
                    }

                    return result;
                }
            }

            if (Regex.IsMatch(content, _TemplateBrace))
            {
                if (Regex.IsMatch(content, _TemplateParentheses))
                {
                    mc = Regex.Matches(content, _TemplateConstructor);

                    if (mc.Count > 0)
                    {
                        // handle constructors with calling base or this
                        result.AddRange(ParseBlocks(mc, content, ParseConstructorBlock, parent));
                        return result;
                    }

                    mc = Regex.Matches(content, PATTERN_Fields);

                    if (mc.Count > 0)
                    {
                        // handle fields, events
                        result.AddRange(ParseBlocks(mc, content, ParseFieldBlock, parent));
                        return result;
                    }

                    mc = Regex.Matches(content, string.Format(@"^([^`;\=]+)({1})\s*({2})*\s*;+|([^`;\=]+)({1})\s*({2})*\s*({0})$",
                                        _TemplateBrace, _TemplateParentheses, _TemplateWhere));

                    if (mc.Count > 0)
                    {
                        // handle methods, delegates, operators, and some constructors
                        result.AddRange(ParseBlocks(mc, content, ParseMethodBasedBlock, parent));
                        return result;
                    }

                    if (content.Contains("operator"))
                    {
                        mc = Regex.Matches(content, string.Format(@"^(````dummy````)(````pattern_type````)(```dummy`````)|([^`;\=]+?[^\w]operator[^\w][^`;]+)({1})\s*({2})*({0})$",
                                            _TemplateBrace, _TemplateParentheses, _TemplateWhere));

                        if (mc.Count > 0)
                        {
                            // handle operators
                            result.AddRange(ParseBlocks(mc, content, ParseMethodBasedBlock, parent));
                            return result;
                        }
                    }
                }
                else
                {
                    mc = Regex.Matches(content, PATTERN_Fields);

                    if (mc.Count > 0)
                    {
                        // handle fields, events
                        result.AddRange(ParseBlocks(mc, content, ParseFieldBlock, parent));
                        return result;
                    } 
                }

                if (content.Contains("class"))
                {
                    mc = Regex.Matches(content, string.Format(@"^([^`]*?)([^`'@]*)class\s+([^`:]+?)({0})|([^`]*?)([^`'@]*)class\s+([^:`]+?):([^`]+)({0})$",
                        _TemplateBrace));

                    if (mc.Count > 0)
                    {
                        // handle classes
                        result.AddRange(ParseClasses(mc, content, parent));
                        return result;
                    }
                }

                if (content.Contains("struct"))
                {
                    mc = Regex.Matches(content, string.Format(@"^([^`]*?)([^`'@]*)struct\s+(\w+)\s*({0})$", _TemplateBrace));

                    if (mc.Count > 0)
                    {
                        // handle structures
                        result.AddRange(ParseStructs(mc, content, parent));
                        return result;
                    }
                }

                if (content.Contains("enum"))
                {
                    mc = Regex.Matches(content, string.Format(@"^([^`]*?)([^`'@]*)enum\s+(\w+)\s*:?\s*(\w*)\s*({0})\s*;*$", _TemplateBrace));

                    if (mc.Count > 0)
                    {
                        // handle enums
                        result.AddRange(ParseBlocks(mc, content, ParseEnumBlock, parent));
                        return result;
                    }
                }

                if (content.Contains("interface"))
                {
                    mc = Regex.Matches(content, string.Format(@"^([^`]*?)([^`'@]*)interface\s+([^`:]+?)({0})|([^`]*?)([^`']*)interface\s+([^:`]+?):([^`]+)({0})$",
                        _TemplateBrace));

                    if (mc.Count > 0)
                    {
                        // handle interfaces
                        result.AddRange(ParseBlocks(mc, content, ParseInterfaceBlock, parent));
                        return result;
                    }
                }

                mc = Regex.Matches(content, string.Format(@"^([^`;]+[^\w][\w]+)\s*({0})|([^`;]+[^\w]this)\s*({1})\s*({0})$",
                    _TemplateBrace, _TemplateBracket));

                if (mc.Count > 0)
                {
                    // handle properties
                    result.AddRange(ParseBlocks(mc, content, ParsePropertyBlock, parent));
                    return result;
                }                               
            }
            else
            {
                if (Regex.IsMatch(content, _TemplateParentheses))
                {
                    mc = Regex.Matches(content, string.Format(@"^([^`;\=]+)({0})\s*({1})*;+$", _TemplateParentheses, _TemplateWhere));

                    if (mc.Count > 0)
                    {
                        // handle abstract methods, delegates
                        result.AddRange(ParseBlocks(mc, content, ParseMethodBasedBlock, parent));
                        return result;
                    }
                }

                mc = Regex.Matches(content, PATTERN_Fields);

                if (mc.Count > 0)
                {
                    // handle fields, events
                    result.AddRange(ParseBlocks(mc, content, ParseFieldBlock, parent));
                    return result;
                }
            }
            
            // it is just a PlainText
            content = TrimCode(content);

            if (content.IsNotEmpty())
            {
                result.Add(new CodeBlockPlainText(0, parent));
                HasUnrecognisedBlocks = true;
            }

            return result;
        }

        private static string TrimCode(string input)
        {
            return input.Trim();
        }

        private CodeBlock DispatchInnerBlocks(CodeBlock block)
        {
            CodeBlock result = null;

            switch (block.Type)
            {
                case CodeBlockType.Namespace:
                case CodeBlockType.Class:
                case CodeBlockType.Region:
                case CodeBlockType.Structure:
                case CodeBlockType.Interface:
                    IList<CodeBlock> blocks = ParseBlockContent(block.Content, block);
                    block.InnerBlocks.Clear();

                    foreach (CodeBlock b in blocks)
                    {
                        block.InnerBlocks.Add(b);
                    }

                    result = block;
                    break;
                case CodeBlockType.Delegate:
                case CodeBlockType.Field:
                case CodeBlockType.Const:
                case CodeBlockType.Event:
                case CodeBlockType.RoutedEvent:
                case CodeBlockType.Method:
                case CodeBlockType.Operator:
                case CodeBlockType.Constructor:
                case CodeBlockType.Destructor:
                case CodeBlockType.Property:
                case CodeBlockType.IndexProperty:
                case CodeBlockType.DependencyProperty:
                case CodeBlockType.Attribute:
                case CodeBlockType.Enum:
                case CodeBlockType.Using:
                case CodeBlockType.Comment:
                case CodeBlockType.SingleLineDirective:
                    result = block;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported CodeBlockType: " + block.ToString());
            }

            return result;
        }

        private IList<CodeBlock> ParseNamespaces(string input)
        {
            string namespacePattern = string.Format(@"[^;`]*namespace\s+([\w\.]+)\s*({0})", _TemplateBrace);
            MatchCollection mc = Regex.Matches(input, namespacePattern);

            if (mc.Count <= 0)
            {
                ThrowException(ExceptionCode.NamespaceNotFound, 1);
            }

            if (mc.Count > 1)
            {
                ThrowException(ExceptionCode.DoubleNamespaceDeclaration, GetLineNumber(input, mc[1].Index));
            }

            string namespaceBody = _ReplacesBraces[mc[0].Groups[2].Value].TrimBraces();
            int lineNumber = GetLineNumber(input, mc[0].Index);

            List<CodeBlock> result = new List<CodeBlock>();
            var preBlocks = ParseBlockContent(input.Substring(0, mc[0].Index), null);
            string header = GetHeaderBefore(input, mc[0], 2);
            string bottom = ExtractFromPackedCode(input.Substring(mc[0].Groups[2].Index + mc[0].Groups[2].Length));

            CodeBlock block = new CodeBlockNamespace(RemoveSpaces(mc[0].Groups[1].Value), namespaceBody, header, bottom, lineNumber, preBlocks.ToArray(), null);

            result.Add(DispatchInnerBlocks(block));

            return result;
        }

        private void ThrowException(ExceptionCode code, int line)
        {
            Debug.WriteLine(String.Format("CodeParser: '{0}' ({1})", _ParsingFilename, line));
            throw new CodeCleanerException(code, line, _ParsingFilename);
        }

        private void ClearResult()
        {
            HasUnrecognisedBlocks = false;
            _ParsingFilename = string.Empty;
            _Strings = new Dictionary<string, string>();
            _ReplacesRegions = new Dictionary<string, string>();
            _ReplacesBraces = new Dictionary<string, string>();
            CodeBlocks = new List<CodeBlock>();
        }

        #endregion

        #endregion

        #region ICodeParser

        /// <summary>
        /// Parses the file specified.
        /// </summary>
        /// <param name="fileName">The name of file.</param>
        /// <returns>The boolean value indicating whether the file may be skipped or not.</returns>
        public bool Parse(string fileName)
        {
            bool res = false;

            FileInfo fi = new FileInfo(fileName);

            ClearResult();

            _ParsingFilename = fi.FullName;
            string result;

            using (StreamReader sr = new StreamReader(fileName))
            {
                result = sr.ReadToEnd();
            }

            if (result.IsNotNullOrEmpty() && result.Length > CC_DIRECTIVE_SKIPPED.Length)
            {
                res = result.Substring(0, CC_DIRECTIVE_SKIPPED.Length).Equals(CC_DIRECTIVE_SKIPPED);
            }

            if (!res)
            {
                Regenerateable = !Regex.IsMatch(result, PATTERN_Directives);

                _ReplacesRegions = new Dictionary<string, string>();
                _ReplacesBraces = new Dictionary<string, string>();
                _ReplacesParenthesis = new Dictionary<string, string>();
                _ReplacesBrackets = new Dictionary<string, string>();
                _ReplacesComments = new Dictionary<string, string>();
                _ReplacesDirectives = new Dictionary<string, string>();
                _ReplacesUsings = new Dictionary<string, string>();

                _Strings = GetAllStrings(ref result);
                result = RemoveDirectives(result);
                result = RemoveUsings(result);
                result = RemoveComments(result);
                _ContentWithoutComments = result;

                result = RemoveBracesAndRegions(result);
                IList<CodeBlock> codeBlocks = ParseNamespaces(result);
                CodeBlocks = codeBlocks;
            }

            return res;
        }

        public IList<CodeBlock> CodeBlocks
        {
            get;
            private set;
        }

        public bool HasUnrecognisedBlocks
        {
            get;
            private set;
        }

        public bool Regenerateable
        {
            get;
            private set;
        }

        #endregion
    }
}
