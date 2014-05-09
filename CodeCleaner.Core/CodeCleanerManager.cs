using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner.Extentions;
using ResourceStrings = CodeCleaner.Core.Properties.Resources;

namespace CodeCleaner
{
    public class CodeCleanerManager
    {
        #region Constants

        private const string LOG_FILE_NAME_Format = "yyyyMMdd";
        private const int MAX_BackUpSize = 1024 * 1024 * 50;
        private const string PATH_OryginResourceRoot = @"\Orygin.ControlPanel.Warehouse\";
        private const string REGEX_Template = "^{0}$";
        private const string SEARCH_PATTERN_Cs = "*.cs";
        private const string SEARCH_PATTERN_Xaml = "*.xaml";
        //xmlns:res="clr-namespace:Orygin.Resources;assembly=Orygin.Resources"
        private const string XAML_RESOURCE_DefaultNamespace = "clr-namespace:Orygin.Resources;assembly=Orygin.Resources";
        private const string XAML_RESOURCE_NameSpaceKey = "xmlns:res";
        private const string XAML_RESOURCE_NamespaceTemplate = "clr-namespace:{0}";
        //Content="{x:Static Member=res:Resources.XAML_InventoryItemLabelPartNumber}"
        private const string XAML_RESOURCE_Template = "{{x:Static Member=res:Resources.{0}}}";

        #endregion

        #region Ctors

        public CodeCleanerManager(ICodeSpecification specification, ICodeParser parser, ICodeCleanerProject project, IFileObserverManager fileObserver)
        {
            Checker.NotNull(specification, "specification");
            Checker.NotNull(project, "project");
            Checker.NotNull(parser, "parser");
            Checker.NotNull(fileObserver, "fileObserver");

            Specification = specification;
            Project = project;
            Parser = parser;
            _FileObserver = fileObserver;

            _XamlValueableAttributesList = new string[] { "Content", "Text", "Header", "Title", "ToolTip" };
            _XamlValueIgnoreList = new string[] { "0", "*", ":", "(", ")", "..." };
            _RegExNormalAttribute = new Regex(@"^\{[^\0]+\}$|^\w$", RegexOptions.Multiline);

            _Worker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _Worker.DoWork += Worker_DoWork;
            _Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            _Worker.ProgressChanged += Worker_ProgressChanged;
        }

        #endregion

        #region Fields

        private bool _CheckQurantine;
        private readonly IFileObserverManager _FileObserver;
        private readonly Regex _RegExNormalAttribute;
        private BackgroundWorker _Worker;
        private readonly string[] _XamlValueableAttributesList;
        private readonly string[] _XamlValueIgnoreList;

        #endregion

        #region Properties

        #region Public

        public bool IsWorking
        {
            get
            {
                return _Worker.IsBusy;
            }
        }

        public ICodeParser Parser
        {
            get;
            private set;
        }

        public ICodeCleanerProject Project
        {
            get;
            private set;
        }

        public ICodeSpecification Specification
        {
            get;
            private set;
        }

        #endregion

        #endregion

        #region Methods

        #region Public

        /// <summary>
        /// Sync analyzing files.
        /// </summary>
        /// <param name="searchPath">Path where files will be found.</param>
        /// <returns>null if no problems</returns>
        public Problem AnalysePath(string searchPath)
        {
            IList<string> filesNames = GetFileNames(searchPath, SEARCH_PATTERN_Cs);
            Problem problem = null;

            for (int i = 0; i < filesNames.Count; i++)
            {
                string currentFilename = filesNames[i];

                try
                {
                    if (!Parser.Parse(currentFilename))
                    {
                        problem = CheckFile(currentFilename);

                        if (problem.IsNotNull())
                        {
                            _FileObserver.RemoveFile(currentFilename);
                            break;
                        }
                        else
                        {
                            _FileObserver.SetFile(currentFilename);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _FileObserver.RemoveFile(currentFilename);
                    problem = new Problem(currentFilename);
                    problem.Issues.Add(new ProblemIssue(string.Format("EXCEPTION while parsing or analyzing file - {0}.", ex.Message), 1, true, ex));
                    break;
                }
            }

            return problem;
        }

        public string[] CheckGeneratingFiles(string sourceDir)
        {
            List<string> problemFileNames = new List<string>();
            var fileNames = GetFileNames(sourceDir, SEARCH_PATTERN_Xaml);
            List<string> problemFiles = new List<string>();
            string tempFile = Path.GetTempFileName() + ".cs";

            foreach (string filePath in fileNames)
            {
                try
                {
                    Debug.WriteLine("File parsing: " + filePath);

                    if (!Parser.Parse(filePath))
                    {

                        if (Parser.HasUnrecognisedBlocks)
                        {
                            Debug.WriteLine("!!! Sent to quarantine: " + filePath);
                            problemFileNames.Add(filePath);
                            continue;
                        }

                        if (!Parser.Regenerateable)
                        {
                            Debug.WriteLine("!!! File is not regenerateable : " + filePath);
                            continue;
                        }

                        CodeBlock blockBefore = Parser.CodeBlocks.First();

                        string newContent = blockBefore.Generate();
                        string oldContent;
                        SaveContent(tempFile, newContent);
                        //Debug.WriteLine("File saved: " + filePath);

                        using (StreamReader sr = new StreamReader(filePath))
                        {
                            oldContent = sr.ReadToEnd();
                        }

                        if (!oldContent.IdenticalTo(newContent))
                        {
                            problemFileNames.Add(filePath);
                            Debug.WriteLine("!!! File generating failed: " + filePath);
                        }

                        // TODO: check when two identical files ot identical
                        //Parser.Parse(tempFile);

                        //if (!blockBefore.CompareTo(Parser.CodeBlocks.First()))
                        //{
                        //    Debug.WriteLine("!!! File generating failed: " + filePath);
                        //}
                    }
                }
                catch (System.Exception ex)
                {
                    problemFileNames.Add(filePath);
                    Debug.WriteLine(string.Format("!!! File parsing failed: {1} ex: {0}", ex.Message, filePath));
                }
            }

            return problemFileNames.ToArray();
        }

        public void NormalizeFileAsync(IList<Problem> problems)
        {
            Checker.NotNull(problems, "problems");
            Checker.IsNotTrue(problems.Any(p => !p.Regenerateable), "Normalize only regenerateable problems.");

            if (!IsWorking)
            {
                _Worker.RunWorkerAsync(problems);
            }
        }

        /// <summary>
        /// Start async analyzing files.
        /// </summary>
        public void StartAsync()
        {
            StartAsync(false);
        }

        public void StartAsync(bool startOnQuarantine)
        {
            if (!IsWorking)
            {
                _Worker.RunWorkerAsync(startOnQuarantine);
            }
        }

        public void StartAsync(string[] files)
        {
            Checker.NotNull(files, "files");
            Checker.IsTrue(files.Length > 0, "files.Length > 0");

            if (!IsWorking)
            {
                _Worker.RunWorkerAsync(files);
            }
        }

        public void StopAsync()
        {
            _Worker.CancelAsync();
        }

        #endregion

        #region Private

        private static string BindingTypeToString(BindingType type)
        {
            return type.ToString();
        }

        private void CheckBlockName(ISpecificationTarget target, CodeBlock block, Problem result)
        {
            if (target.NameConvention.IsNotNullOrEmpty() && !Regex.IsMatch(block.Name, string.Format(REGEX_Template, target.NameConvention)))
            {
                result.Issues.Add(new ProblemIssue(string.Format("{0} '{1}' breaks name convention '{2}'.",
                    CodeBlockTypeToString(block.Type), block.Name, target.NameConvention), block.LineNumber));
            }
        }

        private bool CheckClassBlock(CodeBlock blockClass, ISpecificationTarget target, Problem result)
        {
            int index = 0;
            CodeBlock notRegion = blockClass.ValuableInnerBlocks.FirstOrDefault(p => p.Type != CodeBlockType.Region);

            if (target.RegionsOnly && notRegion.IsNotNull())
            {
                result.Issues.Add(new ProblemIssue(string.Format("{0} '{3}' can contain only regions. But found {1} '{2}'.",
                            BindingTypeToString(target.BindingType), CodeBlockTypeToString(notRegion.Type), notRegion.Name, blockClass.Name),
                            notRegion.LineNumber));
            }

            IList<CodeBlock> regionBlocks = blockClass.ValuableInnerBlocks.Where(p => p.Type == CodeBlockType.Region).ToList();
            IList<Region> existingRegions = target.Regions.Where(p => p.RegionNameConvention.IsNotNullOrEmpty() || regionBlocks.Any(r => r.Name == p.Name)).ToList();

            CheckRepeatedRegionNames(regionBlocks, result);

            foreach (var region in existingRegions)
            {
                if (index >= regionBlocks.Count)
                {
                    break;
                }

                if (region.RegionNameConvention.IsNullOrEmpty())
                {
                    if (regionBlocks[index].Name == region.Name)
                    {
                        if (!CheckRegionBlock(regionBlocks[index], region, result, null))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        CodeBlock foundRegion = regionBlocks.FirstOrDefault(p => p.Name == region.Name);

                        if (foundRegion.IsNotNull())
                        {
                            if (!CheckRegionBlock(foundRegion, region, result, null))
                            {
                                return false;
                            }
                        }

                        if (regionBlocks[index].Name.IsNotEmpty())
                        {
                            result.Issues.Add(new ProblemIssue(string.Format(ResourceStrings.PROBLEM_RegionExpectedButFound,
                                CodeBlockTypeToString(blockClass.Type), blockClass.Name,
                                region.Name, regionBlocks[index].Name), regionBlocks[index].LineNumber));
                        }
                        else
                        {
                            result.Issues.Add(new ProblemIssue(string.Format(ResourceStrings.PROBLEM_RegionEmptyNameFound,
                                CodeBlockTypeToString(blockClass.Type), blockClass.Name,
                                regionBlocks[index].Name), regionBlocks[index].LineNumber));
                        }
                    }
                }
                else
                {
                    int maxIndex = region.MaxRegionRepeatCount <= 0 ? regionBlocks.Count - 1 : region.MaxRegionRepeatCount - 1;

                    for (int i = index; i <= maxIndex; i++)
                    {
                        if (Regex.IsMatch(regionBlocks[i].Name, string.Format(REGEX_Template, region.RegionNameConvention)))
                        {
                            if (!CheckRegionBlock(regionBlocks[i], region, result, null))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            result.Issues.Add(new ProblemIssue(string.Format("Region '{2}' in {0} '{1}' breaks name convention '{3}'.",
                               BindingTypeToString(target.BindingType), blockClass.Name, regionBlocks[i].Name, region.RegionNameConvention),
                               regionBlocks[i].LineNumber));
                        }
                    }

                    index = maxIndex;
                }

                index++;
            }

            if (index < regionBlocks.Count)
            {
                for (int i = index; i < regionBlocks.Count; i++)
                {
                    result.Issues.Add(new ProblemIssue(string.Format("{0} '{1}' cannot contain region '{2}' or one has wrong name.",
                            BindingTypeToString(target.BindingType), blockClass.Name, regionBlocks[i].Name),
                            regionBlocks[i].LineNumber));
                }
            }

            return true;
        }

        private bool CheckContainerBlock(CodeBlock block, Problem result)
        {
            if (block.Type == CodeBlockType.Class && Specification.Targets.Any(p => p.BindingType == BindingType.Class))
            {
                ISpecificationTarget classTarget = Specification.Targets.First(p => p.BindingType == BindingType.Class);

                CheckBlockName(classTarget, block, result);

                return CheckClassBlock(block, classTarget, result);
            }
            else if (block.Type == CodeBlockType.Interface && Specification.Targets.Any(p => p.BindingType == BindingType.Interface))
            {
                ISpecificationTarget interfaceTarget = Specification.Targets.First(p => p.BindingType == BindingType.Interface);

                CheckBlockName(interfaceTarget, block, result);
            }
            else if (block.Type == CodeBlockType.Structure && Specification.Targets.Any(p => p.BindingType == BindingType.Struct))
            {
                ISpecificationTarget structTarget = Specification.Targets.First(p => p.BindingType == BindingType.Struct);

                CheckBlockName(structTarget, block, result);
            }

            return true;
        }

        private Problem CheckFile(string filename)
        {
            CodeBlock namespce = Parser.CodeBlocks[0];
            Problem problem = new Problem(filename, namespce, Parser.Regenerateable);

            if (namespce.HasInnerBlocks)
            {
                ISpecificationTarget target = Specification.Targets.FirstOrDefault(p => p.BindingType == BindingType.Namespace);
                List<CodeBlock> containers = namespce.ValuableInnerBlocks.Where(p => p.IsContainerType).ToList();

                if (target.IsNotNull() && target.MaxBlocksCount > 0 && containers.Any()
                    && (containers.Count > target.MaxBlocksCount || containers.Count != namespce.ValuableInnerBlocks.Count()))
                {
                    problem.Issues.Add(new ProblemIssue(string.Format("Namespace {2} can contain only {0} block(s) but has {1}.",
                        target.MaxBlocksCount, namespce.ValuableInnerBlocks.Count, namespce.Name), namespce.LineNumber));
                }

                foreach (CodeBlock block in namespce.ValuableInnerBlocks)
                {
                    if (!CheckContainerBlock(block, problem))
                    {
                        break;
                    }
                }
            }
            else
            {
                problem.Issues.Add(new ProblemIssue("Empty namespace found.", namespce.LineNumber));
            }

            //CheckMentalErrors(namespce, problem);
            CheckOrderProblems(namespce, problem);

            return problem.Issues.Any() ? problem : null;
        }

        [Obsolete("Not used any more. Duplicates MS Code Contracts functionality")]
        private void CheckMentalErrors(CodeBlock block, Problem problem)
        {
            if (block.Type == CodeBlockType.Constructor/* || block.Type == CodeBlockType.Method*/)
            {
                CodeBlockMethodBase methodBased = block.To<CodeBlockMethodBase>();

                foreach (var arg in methodBased.Arguments)
                {
                    Match m = Regex.Match(arg, @"^[^\0]+[^\w](\w+)$");

                    if (!m.Success)
                    {
                        throw new InvalidOperationException("Invalid argument parsing.");
                    }

                    string argName = m.Groups[1].Value;
                    string pattern = string.Format(@"[^\w]{0}[^\w]", argName);

                    if (!Regex.IsMatch(methodBased.Content, pattern))
                    {
                        if (block.Type == CodeBlockType.Method)
                        {
                            CodeBlockMethod method = block.To<CodeBlockMethod>();

                            if (!method.IsAbstract)
                            {
                                problem.Issues.Add(new ProblemIssue(string.Format("Argument '{0}' in method '{1}' not used",
                                       argName, methodBased.Name), methodBased.LineNumber));
                            }
                        }
                        else if (block.Type == CodeBlockType.Constructor)
                        {
                            CodeBlockConstructor constructor = block.To<CodeBlockConstructor>();

                            if (!Regex.IsMatch(constructor.AdditionalCallName, pattern))
                            {
                                problem.Issues.Add(new ProblemIssue(string.Format("Argument '{0}' in constructor '{1}' not used",
                                   argName, constructor.Name), constructor.LineNumber));
                            }
                        }
                    }
                    else if (Regex.IsMatch(methodBased.Content, string.Format(@"[^\w]{0}\s*=\s*{0}[^w]", argName)))
                    {
                        problem.Issues.Add(new ProblemIssue(string.Format("Argument '{0}' in method '{1}' not assigned to itself.",
                                       argName, methodBased.Name), methodBased.LineNumber));
                    }
                    //Debug.WriteLine(string.Format("In {0} found argument: {1}", constructor.Name, arg));
                }
            }

            foreach (var child in block.ValuableInnerBlocks)
            {
                CheckMentalErrors(child, problem);
            }
        }

        private void CheckOrderProblems(CodeBlock block, Problem problem)
        {
            bool runOnChildren = false;
            bool hasProblem = false;

            switch (block.Type)
            {
                case CodeBlockType.Region:
                case CodeBlockType.Interface:
                case CodeBlockType.Structure:
                    {
                        if (block.ValuableInnerBlocks.Any(p => p.Type == CodeBlockType.Region))
                        {
                            runOnChildren = true;
                        }
                        else if (block.ValuableInnerBlocks.Any())
                        {
                            Dictionary<CodeBlockType, CodeBlock> firstsAfterBlock = new Dictionary<CodeBlockType, CodeBlock>();
                            List<CodeBlockType> existingBlockTypes = new List<CodeBlockType>();
                            CodeBlockType currentType = CodeBlockType.None;

                            #region Stage 1: Ordering inner blocks

                            foreach (CodeBlock child in block.ValuableInnerBlocks)
                            {
                                if (child.Type != currentType)
                                {
                                    if (firstsAfterBlock.ContainsKey(child.Type))
                                    {
                                        CodeBlock blockBefore = firstsAfterBlock[child.Type];
                                        problem.Issues.Add(new ProblemIssue(string.Format("{0} '{1}' must be picked up before {2} '{3}'.",
                                            CodeBlockTypeToString(child.Type).UpFirstChar(), child.Name,
                                            CodeBlockTypeToString(blockBefore.Type), blockBefore.Name),
                                            child.LineNumber, IssueType.Order));
                                        hasProblem = true;
                                    }
                                    else
                                    {
                                        if (currentType != CodeBlockType.None)
                                        {
                                            firstsAfterBlock.Add(currentType, child);
                                        }
                                        currentType = child.Type;
                                        existingBlockTypes.Add(currentType);
                                    }
                                }
                            }

                            #endregion

                            #region Stage 2: Ordering groups of similar block

                            if (!hasProblem)
                            {
                                // check order of groups of block 
                                CodeBlockType[] rightTypesOrder = GetRightTypesOrder(block, existingBlockTypes);

                                if (rightTypesOrder.IsNotNull())
                                {
                                    int index = 0;

                                    foreach (var bType in existingBlockTypes)
                                    {
                                        if (index < rightTypesOrder.Length)
                                        {
                                            if (bType != rightTypesOrder[index])
                                            {
                                                CodeBlock wrongBlock = block.ValuableInnerBlocks.First(p => p.Type == bType);
                                                CodeBlock rightBlock = block.ValuableInnerBlocks.First(p => p.Type == rightTypesOrder[index]);

                                                problem.Issues.Add(new ProblemIssue(string.Format("{0} '{1}' and other items of '{0}' must be picked up before {2} '{3}'.",
                                                CodeBlockTypeToString(rightBlock.Type).UpFirstChar(), rightBlock.Name,
                                                CodeBlockTypeToString(wrongBlock.Type), wrongBlock.Name),
                                                wrongBlock.LineNumber, IssueType.Order));
                                                hasProblem = true;
                                                break;
                                            }

                                            index++;
                                        }
                                        else
                                        {
                                            CodeBlock wrongBlock = block.ValuableInnerBlocks.First(p => p.Type == bType);

                                            problem.Issues.Add(new ProblemIssue(string.Format("Found {0} '{1}' located in wrong place.",
                                            CodeBlockTypeToString(wrongBlock.Type).UpFirstChar(), wrongBlock.Name),
                                            wrongBlock.LineNumber, IssueType.Order));
                                            hasProblem = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Stage 3: Sort blocks in each group

                            if (!hasProblem && block.ValuableInnerBlocks.Any())
                            {
                                ISpecificationTarget target = GetTarget(block);

                                foreach (var bType in existingBlockTypes)
                                {
                                    CodeBlock[] existingBlocks = block.ValuableInnerBlocks.Where(p => p.Type == bType).ToArray();

                                    if (existingBlocks.Length > 1)
                                    {
                                        CodeBlock[] rightOrderBlocks = (target.SortType == SortType.Asc ? existingBlocks.OrderBy(p => p.Name)
                                            : existingBlocks.OrderByDescending(p => p.Name)).ToArray();

                                        for (int i = 0; i < existingBlocks.Length; i++)
                                        {
                                            CodeBlock wrongBlock = existingBlocks[i];
                                            CodeBlock rightBlock = rightOrderBlocks[i];

                                            if (wrongBlock != rightBlock)
                                            {
                                                string rightString = rightOrderBlocks.Aggregate(new StringBuilder(Environment.NewLine), (res, next)
                                                    => res.AppendFormat("\t{1}{0}", Environment.NewLine, next.Name)).ToString();

                                                problem.Issues.Add(new ProblemIssue(string.Format("Found {0} '{1}' instead '{2}'.{3} Right order:{4}",
                                                   CodeBlockTypeToString(wrongBlock.Type), wrongBlock.Name,
                                                   rightBlock.Name, Environment.NewLine, rightString),
                                                   wrongBlock.LineNumber, IssueType.Order));
                                                hasProblem = true;
                                                break;
                                            }
                                        }

                                        if (hasProblem)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                    }
                    break;
                default:
                    runOnChildren = true;
                    break;
            }

            if (runOnChildren)
            {
                foreach (CodeBlock child in block.ValuableInnerBlocks)
                {
                    CheckOrderProblems(child, problem);
                }
            }
        }

        private bool CheckRegionBlock(CodeBlock blockRegion, Region region, Problem result, Region parentRegion)
        {
            Checker.AreEqual(blockRegion.Type, CodeBlockType.Region);

            if (!blockRegion.ValuableInnerBlocks.Any())
            {
                result.Issues.Add(new ProblemIssue(string.Format("Region '{0}' is empty.",
                                    blockRegion.Name), blockRegion.LineNumber));
                return true;
            }

            Region synthesized = region.Synthesize(parentRegion);

            // region with InnerRegionsOrder can contains only regions
            if (region.HasInnerRegionsOrder)
            {
                #region Handle Inner Regions

                var notRegionBlock = blockRegion.ValuableInnerBlocks.FirstOrDefault(p => p.Type != CodeBlockType.Region);

                if (notRegionBlock.IsNotNull())
                {
                    result.Issues.Add(new ProblemIssue(string.Format("This region can contain only regions. Region '{0}' cannot contain {1} '{2}'.",
                                    region.Name, CodeBlockTypeToString(notRegionBlock.Type), notRegionBlock.Name),
                                    notRegionBlock.LineNumber));
                }
                else
                {
                    CheckRepeatedRegionNames(blockRegion.ValuableInnerBlocks, result);

                    var innerRegions = region.InnerRegionsOrder.Regions.Where(p => blockRegion.ValuableInnerBlocks.Any(b => b.Name == p.Name)).ToList();
                    bool hasOrderError = false;

                    for (int i = 0; i < Math.Min(innerRegions.Count, blockRegion.ValuableInnerBlocks.Count); i++)
                    {
                        if (innerRegions[i].Name == blockRegion.ValuableInnerBlocks[i].Name)
                        {
                            if (!CheckRegionBlock(blockRegion.ValuableInnerBlocks[i], innerRegions[i], result, synthesized))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (blockRegion.ValuableInnerBlocks[i].Name.IsNotEmpty())
                            {
                                result.Issues.Add(new ProblemIssue(string.Format(ResourceStrings.PROBLEM_RegionExpectedButFound,
                                  CodeBlockTypeToString(blockRegion.Type), blockRegion.Name,
                                  innerRegions[i].Name, blockRegion.ValuableInnerBlocks[i].Name),
                                  blockRegion.ValuableInnerBlocks[i].LineNumber));
                            }
                            else
                            {
                                result.Issues.Add(new ProblemIssue(string.Format(ResourceStrings.PROBLEM_RegionEmptyNameFound,
                                                               CodeBlockTypeToString(blockRegion.Type), blockRegion.Name,
                                                               blockRegion.ValuableInnerBlocks[i].Name), blockRegion.ValuableInnerBlocks[i].LineNumber));
                            }
                            hasOrderError = true;
                            break;
                        }
                    }

                    if (!hasOrderError)
                    {
                        for (int i = innerRegions.Count; i < blockRegion.ValuableInnerBlocks.Count; i++)
                        {
                            result.Issues.Add(new ProblemIssue(string.Format("Region '{0}' cannot be in region '{1}'.",
                                blockRegion.ValuableInnerBlocks[i].Name, blockRegion.Name), blockRegion.ValuableInnerBlocks[i].LineNumber));
                        }
                    }
                }

                #endregion
            }
            else
            {
                if (synthesized.Types.Any())
                {
                    IList<CodeBlockType> allowedTypes = synthesized.Types.Select(p => FromRegionType(p)).ToList();
                    string allowedTypesString = allowedTypes.Aggregate<CodeBlockType, string>(string.Empty,
                        (res, next) => res.Length > 0 ? res + ", " + CodeBlockTypeToString(next) : CodeBlockTypeToString(next));
                    var notAllowedBlocks = blockRegion.ValuableInnerBlocks.Where(p => !allowedTypes.Any(a => p.Type == a));

                    foreach (CodeBlock notAllowedBlock in notAllowedBlocks)
                    {
                        result.Issues.Add(new ProblemIssue(string.Format("Region '{0}' cannot contain {1} '{2}'. This region can contain only: {3}.",
                                    region.Name, CodeBlockTypeToString(notAllowedBlock.Type), notAllowedBlock.Name, allowedTypesString),
                                    notAllowedBlock.LineNumber));
                    }
                }

                if (synthesized.NameConvention.IsNotNullOrEmpty())
                {
                    var notAllowedBlocks = blockRegion.ValuableInnerBlocks.Where(p => p.Name.IsNotEmpty() && p.Type != CodeBlockType.Operator
                        && !Regex.IsMatch(p.Name, string.Format(REGEX_Template, synthesized.NameConvention)));

                    foreach (CodeBlock notAllowedBlock in notAllowedBlocks)
                    {
                        result.Issues.Add(new ProblemIssue(string.Format("{0} '{1}' breaks Name Convention - '{2}'.",
                                    CodeBlockTypeToString(notAllowedBlock.Type), notAllowedBlock.Name, synthesized.NameConvention),
                                    notAllowedBlock.LineNumber));
                    }
                }

                if (!synthesized.AllowFieldAssign)
                {
                    var notAllowedBlocks = blockRegion.ValuableInnerBlocks.Where(p => p.Type == CodeBlockType.Field).Cast<CodeBlockField>().Where(p => p.RightSide.IsNotNullOrEmpty());

                    foreach (CodeBlock notAllowedBlock in notAllowedBlocks)
                    {
                        result.Issues.Add(new ProblemIssue(string.Format("In region '{0}' assigning value to '{1}' is restricted. Case: '{2}'.",
                                    region.Name, CodeBlockTypeToString(notAllowedBlock.Type), notAllowedBlock.Content),
                                    notAllowedBlock.LineNumber));
                    }
                }

                if (synthesized.Modificators.Any())
                {
                    string allowedModificators = synthesized.Modificators.Aggregate<ModificatorType, string>(string.Empty,
                        (res, next) => res.Length > 0 ? res + ", " + CodeBlockModificatorToString(next) : CodeBlockModificatorToString(next));
                    var notAllowedBlocks = blockRegion.ValuableInnerBlocks.Where(p => p.Type != CodeBlockType.Region
                        && (IsBlockCannotBeDafault(p) && !synthesized.Modificators.Any(m => m == p.Modificator)));

                    foreach (var notAllowedBlock in notAllowedBlocks)
                    {
                        result.Issues.Add(new ProblemIssue(string.Format("In region '{0}' found {1} '{2}' with restricted access modificator '{3}'. Allowed modificators: {4}.",
                                    region.Name, CodeBlockTypeToString(notAllowedBlock.Type), notAllowedBlock.Name,
                                    CodeBlockModificatorToString(notAllowedBlock.Modificator),
                                    allowedModificators), notAllowedBlock.LineNumber));
                    }
                }

                foreach (var childBlock in blockRegion.ValuableInnerBlocks)
                {
                    if (!CheckContainerBlock(childBlock, result))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void CheckRepeatedRegionNames(IList<CodeBlock> regions, Problem result)
        {
            List<int> alreadyAdded = new List<int>();

            for (int i = 0; i < regions.Count; i++)
            {
                for (int j = i + 1; j < regions.Count; j++)
                {
                    CodeBlock locRegion = regions[j];

                    if (locRegion.Name == regions[i].Name && !alreadyAdded.Contains(locRegion.LineNumber))
                    {
                        alreadyAdded.Add(locRegion.LineNumber);
                        result.Issues.Add(new ProblemIssue(string.Format("Found region with repeated name '{0}'.", locRegion.Name), locRegion.LineNumber));
                    }
                }
            }
        }

        private Problem CheckXamlFile(string filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            Problem problem = new Problem(filename, true);

            CheckXamlNode(doc.DocumentElement, problem);

            return problem.Issues.Any() ? problem : null;
        }

        private void CheckXamlNode(XmlNode node, Problem problem)
        {
            WrapXamlNode(node, (param) =>
            {
                var nameAttr = param.Value;

                foreach (var attr in param.Key)
                {
                    StringBuilder message = new StringBuilder();
                    IssueType issueType;

                    if (attr.Value.IsNotEmpty())
                    {
                        message.AppendFormat("Attribute '{0}' with hardcoded string \"{1}\" found in node ", attr.Name, attr.Value);
                        issueType = IssueType.XamlHardcodedStrings;
                    }
                    else
                    {
                        message.AppendFormat("Attribute '{0}' with EMPTY string found in node ", attr.Name);
                        issueType = IssueType.Normal;
                    }

                    if (nameAttr.IsNotNull())
                    {
                        message.AppendFormat("'{0}' ('{1}')", nameAttr.Value, GetNodePath(node));
                    }
                    else
                    {
                        message.AppendFormat("'{0}'", GetNodePath(node));
                    }

                    problem.Issues.Add(new ProblemIssue(message.ToString(), 0, issueType));
                }
            });
        }

        private static string CodeBlockModificatorToString(ModificatorType type)
        {
            return type.ToString().ToLower();
        }

        private static string CodeBlockTypeToString(CodeBlockType type)
        {
            if (type == CodeBlockType.IndexProperty)
            {
                return "Index Property";
            }

            return type.ToString().ToLower();
        }

        private void CreateDirectory(string path)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);

            if (!dirInfo.Exists)
            {
                if (dirInfo.Parent.Exists)
                {
                    dirInfo.Create();
                }
                else
                {
                    CreateDirectory(dirInfo.Parent.FullName);
                    dirInfo.Create();
                }
            }
        }

        private void FixOrderProblems(IList<Problem> problems)
        {
            foreach (Problem problem in problems)
            {
                FixOrderProblems(problem.RootBlock);
            }
        }

        private void FixOrderProblems(CodeBlock block)
        {
            bool runOnChildren = false;

            switch (block.Type)
            {
                case CodeBlockType.Region:
                case CodeBlockType.Interface:
                case CodeBlockType.Structure:
                    if (block.ValuableInnerBlocks.Any(p => p.Type == CodeBlockType.Region))
                    {
                        runOnChildren = true;
                    }
                    else if (block.ValuableInnerBlocks.Any())
                    {
                        IEnumerable<CodeBlockType> existingBlockTypes = block.ValuableInnerBlocks.Select(p => p.Type).Distinct();
                        CodeBlockType[] rightTypesOrder = GetRightTypesOrder(block, existingBlockTypes);

                        if (rightTypesOrder.Length > 0)
                        {
                            IList<CodeBlock> innerBlocks = block.InnerBlocks;
                            ISpecificationTarget target = GetTarget(block);
                            List<CodeBlock> fixedOrderedBlocks = new List<CodeBlock>();
                            List<CodeBlock> lastCommentsAndDirectives = new List<CodeBlock>();
                            var blockCommentsAndDirectives = new Dictionary<CodeBlock, IList<CodeBlock>>();
                            CodeBlock lastBlock = null;

                            for (int i = innerBlocks.Count - 1; i >= 0; i--)
                            {
                                CodeBlock currentBlock = innerBlocks[i];

                                if (currentBlock.Type != CodeBlockType.Comment && currentBlock.Type != CodeBlockType.SingleLineDirective)
                                {
                                    lastBlock = currentBlock;
                                }
                                else if (lastBlock.IsNotNull())
                                {
                                    if (!blockCommentsAndDirectives.ContainsKey(lastBlock))
                                    {
                                        blockCommentsAndDirectives.Add(lastBlock, new List<CodeBlock>());
                                    }
                                    blockCommentsAndDirectives[lastBlock].Insert(0, currentBlock);
                                }
                                else
                                {
                                    lastCommentsAndDirectives.Insert(0, currentBlock);
                                }
                            }

                            foreach (var bType in rightTypesOrder)
                            {
                                var group = innerBlocks.Where(p => p.Type == bType);
                                fixedOrderedBlocks.AddRange((target.SortType == SortType.Asc ? group.OrderBy(p => p.Name) : group.OrderByDescending(p => p.Name)).ToList());
                            }

                            foreach (var tblock in fixedOrderedBlocks.ToList())
                            {
                                if (blockCommentsAndDirectives.ContainsKey(tblock))
                                {
                                    int index = fixedOrderedBlocks.FindIndex(p => p == tblock);
                                    fixedOrderedBlocks.InsertRange(index, blockCommentsAndDirectives[tblock]);
                                }
                            }

                            fixedOrderedBlocks.AddRange(lastCommentsAndDirectives);

                            block.InnerBlocks = fixedOrderedBlocks;
                        }
                    }
                    break;
                default:
                    runOnChildren = true;
                    break;
            }

            if (runOnChildren)
            {
                foreach (CodeBlock child in block.ValuableInnerBlocks)
                {
                    FixOrderProblems(child);
                }
            }
        }

        private void FixXamlFile(string filename)
        {
            string resourceFile = GetProjectResourceFile(filename);

            if (resourceFile.IsNotNullOrEmpty())
            {
                IDictionary<string, string> values = new Dictionary<string, string>();
                XmlDocument doc = new XmlDocument();
                doc.Load(filename);
                var resStrings = CodeCleaner.Helpers.ResourceHelper.GetLangSymbols(resourceFile);
                string prefix = "XAML_" + GetClassName(doc.DocumentElement);
                string resNamespace = GetResourceNamespace(resourceFile);

                WrapXamlNode(doc.DocumentElement, (param) =>
                {
                    var nameAttr = param.Value;

                    foreach (var attr in param.Key.Where(p => p.Value.IsNotEmpty()))
                    {
                        string valueName = MakeAttributeValueName(attr, nameAttr, prefix, (name) =>
                            {
                                return !values.ContainsKey(name) && !resStrings.ContainsKey(name);
                            });

                        values.Add(valueName, attr.Value);
                        attr.Value = string.Format(XAML_RESOURCE_Template, valueName);
                    }
                });

                if (values.Count > 0)
                {
                    if (doc.DocumentElement.Attributes[XAML_RESOURCE_NameSpaceKey].IsNull())
                    {
                        var attrRes = doc.CreateAttribute(XAML_RESOURCE_NameSpaceKey);
                        attrRes.Value = resNamespace;
                        doc.DocumentElement.Attributes.Append(attrRes);
                    }

                    CodeCleaner.Helpers.ResourceHelper.AddLangSymbols(resourceFile, values);
                    doc.Save(filename);
                }
            }
        }

        private static BindingType FromCodeBlockType(CodeBlockType codeBlockType)
        {
            switch (codeBlockType)
            {
                case CodeBlockType.Interface:
                    return BindingType.Interface;
                case CodeBlockType.Class:
                    return BindingType.Class;
                case CodeBlockType.Structure:
                    return BindingType.Struct;
                case CodeBlockType.Namespace:
                    return BindingType.Namespace;
                default:
                    return BindingType.None;
            }
        }

        private static CodeBlockType FromRegionType(RegionType regionType)
        {
            switch (regionType)
            {
                case RegionType.Class:
                    return CodeBlockType.Class;
                case RegionType.Constructor:
                    return CodeBlockType.Constructor;
                case RegionType.Destructor:
                    return CodeBlockType.Destructor;
                case RegionType.Delegate:
                    return CodeBlockType.Delegate;
                case RegionType.Enum:
                    return CodeBlockType.Enum;
                case RegionType.Event:
                    return CodeBlockType.Event;
                case RegionType.RoutedEvent:
                    return CodeBlockType.RoutedEvent;
                case RegionType.Field:
                    return CodeBlockType.Field;
                case RegionType.Interface:
                    return CodeBlockType.Interface;
                case RegionType.Method:
                    return CodeBlockType.Method;
                case RegionType.Operator:
                    return CodeBlockType.Operator;
                case RegionType.Property:
                    return CodeBlockType.Property;
                case RegionType.IndexProperty:
                    return CodeBlockType.IndexProperty;
                case RegionType.DependencyProperty:
                    return CodeBlockType.DependencyProperty;
                case RegionType.Struct:
                    return CodeBlockType.Structure;
                case RegionType.Const:
                    return CodeBlockType.Const;
                default:
                    throw new InvalidOperationException("Unsupported RegionType: " + regionType.ToString());
            }
        }

        private static string GetClassName(XmlNode node)
        {
            var attrClass = node.Attributes["x:Class"];

            if (attrClass.IsNotNull())
            {
                int index = attrClass.Value.LastIndexOf('.');

                if (index >= 0)
                {
                    return attrClass.Value.Substring(index + 1);
                }
                else
                {
                    return attrClass.Value;
                }
            }

            return string.Empty;
        }

        private IList<string> GetFileNames(string forceSearchPath, string searchPattern)
        {
            string searchPath = forceSearchPath;

            if (searchPath.IsNullOrEmpty())
            {
                if (_CheckQurantine)
                {
                    if (Project.QuarantineOutputPath.IsNullOrEmpty())
                    {
                        throw new InvalidOperationException("Quarantine path not specified");
                    }

                    searchPath = Project.QuarantineOutputPath;
                }
                else
                {
                    if (Project.FilesSearchPaths.Count <= 0)
                    {
                        throw new InvalidOperationException("Search path not specified");
                    }

                    searchPath = Project.FilesSearchPaths.First();
                }
            }

            DirectoryInfo dirInfo = new DirectoryInfo(searchPath);
            FileInfo[] files = dirInfo.GetFiles(searchPattern, SearchOption.AllDirectories);

            List<string> result = new List<string>();

            foreach (FileInfo fi in files)
            {
                if (Project.ExcludeFilenamePatterns.Any(p => Regex.IsMatch(fi.Name, string.Format(REGEX_Template, p))))
                {
                    continue;
                }

                if (Project.IncludeFilenamePatterns.Any())
                {
                    if (!Project.IncludeFilenamePatterns.Any(p => Regex.IsMatch(fi.Name, string.Format(REGEX_Template, p))))
                    {
                        continue;
                    }
                }

                if (_FileObserver.IsChanged(fi.FullName))
                {
                    result.Add(fi.FullName);
                }
            }

            return result;
        }

        private string GetNodePath(XmlNode node)
        {
            List<XmlNode> list = new List<XmlNode>();
            XmlNode parent = node;

            while (parent.IsNotNull())
            {
                list.Insert(0, parent);
                parent = parent.ParentNode;
            }

            StringBuilder result = new StringBuilder();

            foreach (var nd in list)
            {
                if (result.Length > 0)
                {
                    result.AppendFormat("/{0}", nd.Name);
                }
                else
                {
                    result.Append(nd.Name);
                }
            }

            return result.ToString();
        }

        private string GetProjectResourceFile(string path)
        {
            if (IsOryginResource(path))
            {
                int index = path.IndexOf(PATH_OryginResourceRoot);

                if (index >= 0)
                {
                    string result = Path.Combine(path.Substring(0, index + PATH_OryginResourceRoot.Length), @"Orygin.Resources\Resources.resx");

                    return result;
                }

                throw new InvalidOperationException("Invalid path for Orygin: " + path);
            }
            else
            {
                if (File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }

                string curDir = path;

                string propertiesFile = Path.Combine(path, @"Properties\Resources.resx");

                if (File.Exists(propertiesFile))
                {
                    return propertiesFile;
                }

                string dirName = Path.GetDirectoryName(curDir);

                if (dirName.Length > 3)
                {
                    return GetProjectResourceFile(dirName);
                }

                return null;
            }
        }

        private static string GetResourceNamespace(string resourceFile)
        {
            if (resourceFile.Contains(@"\Orygin.Resources\Resources.resx"))
            {
                return XAML_RESOURCE_DefaultNamespace;
            }
            else
            {
                string codeFile = Path.GetFileNameWithoutExtension(resourceFile) + ".Designer.cs";
                codeFile = Path.Combine(Path.GetDirectoryName(resourceFile), codeFile);

                if (File.Exists(codeFile))
                {
                    using (var sr = new StreamReader(codeFile))
                    {
                        Regex rgx = new Regex(@"namespace\s+([\w\.]+)");
                        string line = sr.ReadLine();

                        while (line.IsNotNull())
                        {
                            var match = rgx.Match(line);

                            if (match.Success)
                            {
                                return string.Format(XAML_RESOURCE_NamespaceTemplate, match.Groups[1].Value.Trim());
                            }

                            line = sr.ReadLine();
                        }
                    }
                }

                throw new InvalidOperationException(string.Format("File not ound: {0}", codeFile));
            }
        }

        private CodeBlockType[] GetRightTypesOrder(CodeBlock block, IEnumerable<CodeBlockType> existingBlockTypes)
        {
            ISpecificationTarget target = GetTarget(block);

            if (target.IsNotNull() && target.TypesOrder.IsNotNull())
            {
                CodeBlockType[] rightTypesOrder = target.TypesOrder.Regions
                        .Select(p => FromRegionType(p.Types.First()))
                        .Where(p => existingBlockTypes.Any(t => p == t)).ToArray();

                return rightTypesOrder;
            }

            return new CodeBlockType[0];
        }

        private ISpecificationTarget GetTarget(CodeBlock block)
        {
            CodeBlock topTargetBlock = block.TopTargetBlock;
            ISpecificationTarget target = Specification.Targets.FirstOrDefault(p => p.BindingType == FromCodeBlockType(topTargetBlock.Type));

            return target;
        }

        /// <summary>
        /// Checks if block can be used with Default modificator
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private static bool IsBlockCannotBeDafault(CodeBlock block)
        {
            if (block.Modificator != ModificatorType.Default)
            {
                return true;
            }

            switch (block.Type)
            {
                case CodeBlockType.Method:
                    return block.To<CodeBlockMethod>().ExplicitInterfaceName.IsEmpty();
                case CodeBlockType.Property:
                case CodeBlockType.IndexProperty:
                    return block.To<CodeBlockPropertyBase>().ExplicitInterfaceName.IsEmpty();
                case CodeBlockType.Event:
                    return block.To<CodeBlockEvent>().ExplicitInterfaceName.IsEmpty();
                default:
                    return true;
            }
        }

        private static bool IsOryginResource(string fileName)
        {
            return fileName.Contains(@"\Orygin.WinGUI\");
        }

        private string MakeAttributeValueName(XmlAttribute attr, XmlAttribute attrName, string prefix, Func<string, bool> handler)
        {
            StringBuilder result = new StringBuilder(prefix);

            if (attrName.IsNotNull())
            {
                result.Append(attrName.Value.UpperFirstChar());
                result.Append(attr.Name);
            }
            else
            {
                XmlNode parentNode = attr.OwnerElement as XmlNode;

                if (parentNode.IsNotNull() && !(parentNode.ParentNode is XmlDocument))
                {
                    result.Append(parentNode.Name.Replace(":", string.Empty));
                }

                result.Append(attr.Name);

                var matches = Regex.Matches(attr.Value, @"\w[\w]+");

                for (int i = 0; i < Math.Min(matches.Count, 3); i++)
                {
                    result.Append(matches[i].Value.UpperFirstChar());
                }
            }

            int index = 0;

            while (!handler(result.ToString()))
            {
                result.Append(++index);
            }

            return result.ToString();
        }

        private void NotifyByException(CodeCleanerException exception)
        {
            Problem problem = new Problem(exception.Filename);

            problem.Issues.Add(new ProblemIssue(exception.Message, exception.Line));

            NotifyProblem(problem);
        }

        private void NotifyProblem(Problem problem)
        {
            _Worker.ReportProgress(-1, problem);
        }

        private void NotifyProgress(string filename, int index, int count)
        {
            _Worker.ReportProgress(Math.Max(1, index * 100 / count), filename);
        }

        /// <summary>
        /// Sends file if file has errors while parsing
        /// </summary>
        /// <param name="fileName"></param>
        private void SendToQuarantine(string fileName, Exception parseCause)
        {
            Problem problem = new Problem(fileName);

            if (Project.QuarantineOutputPath.IsNotNull())
            {
                string searchPath = Project.FilesSearchPaths.First().Trim('\\');
                FileInfo fInfo = new FileInfo(fileName.Replace(searchPath, Project.QuarantineOutputPath));

                CreateDirectory(fInfo.DirectoryName);

                problem.Issues.Add(new ProblemIssue(string.Format("QUARATINE: File failed while parsing and copied to '{0}'",
                    fInfo.FullName), 0, true, parseCause));

                if (!_CheckQurantine)
                {
                    File.Copy(fileName, fInfo.FullName, true);
                }
            }
            else
            {
                problem.Issues.Add(new ProblemIssue(string.Format("QUARATINE directory not defined: File failed while parsing '{0}'",
                    fileName), 0, true, null));
            }

            NotifyProblem(problem);
        }

        private void WrapXamlNode(XmlNode node, Action<KeyValuePair<IEnumerable<XmlAttribute>, XmlAttribute>> handler)
        {
            if (node.Attributes.IsNotNull() && node.Attributes.Count > 0)
            {
                XmlAttribute nameAttr = node.Attributes["x:Name"] ?? node.Attributes["Name"];
                var attrList = node.Attributes.OfType<XmlAttribute>().Where(p => _XamlValueableAttributesList.Contains(p.Name))
                    .Where(p => !_RegExNormalAttribute.IsMatch(p.Value) && !_XamlValueIgnoreList.Contains(p.Value));

                handler(new KeyValuePair<IEnumerable<XmlAttribute>, XmlAttribute>(attrList, nameAttr));
            }

            foreach (XmlNode childNode in node.ChildNodes)
            {
                WrapXamlNode(childNode, handler);
            }
        }

        #endregion

        #endregion

        #region Event Handlers

        private void BackUpFile(string filePath)
        {
            string onlyFileName = Path.GetFileNameWithoutExtension(filePath);
            string fileExt = Path.GetExtension(filePath);
            DateTime now = DateTime.Now;
            string newFilePath = Path.Combine(Project.BackUpOutputPath, string.Format("{0}_{1}{2}", now.ToString(LOG_FILE_NAME_Format), onlyFileName, fileExt));
            int index = 1;

            while (File.Exists(newFilePath))
            {
                newFilePath = Path.Combine(Project.BackUpOutputPath,
                    string.Format("{0}_{1}[{3}]{2}", now.ToString(LOG_FILE_NAME_Format), onlyFileName, fileExt, index++));
            }

            if (!Directory.Exists(Project.BackUpOutputPath))
            {
                Directory.CreateDirectory(Project.BackUpOutputPath);
            }

            File.Copy(filePath, newFilePath, true);
            File.SetLastWriteTime(newFilePath, DateTime.Now);

            DeleteOldBackUpFiles();
        }

        private bool CheckAndNotifyFile(string currentFilename)
        {
            if (!Parser.Parse(currentFilename))
            {
                if (_Worker.CancellationPending)
                {
                    return false;
                }

                if (Parser.HasUnrecognisedBlocks)
                {
                    SendToQuarantine(currentFilename, new InvalidOperationException("Unrecognised blocks found."));
                    return true;
                }

                Problem problem = CheckFile(currentFilename);

                if (_Worker.CancellationPending)
                {
                    return false;
                }

                if (problem.IsNotNull())
                {
                    _FileObserver.RemoveFile(currentFilename);
                    NotifyProblem(problem);
                }
                else
                {
                    _FileObserver.SetFile(currentFilename);
                }
            }
            else
            {
                _FileObserver.SetFile(currentFilename);
            }

            return true;
        }

        private bool CheckAndNotifyXamlFile(string fileName)
        {
            if (_Worker.CancellationPending)
            {
                return false;
            }

            Problem problem = CheckXamlFile(fileName);

            if (_Worker.CancellationPending)
            {
                return false;
            }

            if (problem.IsNotNull())
            {
                _FileObserver.RemoveFile(fileName);
                NotifyProblem(problem);
            }
            else
            {
                _FileObserver.SetFile(fileName);
            }

            return true;
        }

        private void DeleteOldBackUpFiles()
        {
            if (Directory.Exists(Project.BackUpOutputPath))
            {
                var files = Directory.GetFiles(Project.BackUpOutputPath).Select(p => new FileInfo(p)).OrderBy(p => p.LastWriteTime).ToList();
                long allSize = files.Select(p => p.Length).Sum();

                if (allSize > MAX_BackUpSize)
                {
                    foreach (var finfo in files)
                    {
                        try
                        {
                            finfo.Delete();
                            allSize -= finfo.Length;

                            if (allSize <= MAX_BackUpSize)
                            {
                                break;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.WriteLine("EXCEPTION: " + ex.ToString());
                        }
                    }
                }
            }
        }

        private void SaveContent(string filename, string newContent)
        {
            using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
            {
                sw.Write(newContent);
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int proccessedFileCount;
            Type argumentType = e.Argument.GetType();

            if (argumentType.GetInterfaces().Any(p => p.Equals(typeof(IList<Problem>))))
            {
                #region Fix problems

                IList<Problem> problems = e.Argument.To<IList<Problem>>();

                for (int i = 0; i < problems.Count; i++)
                {
                    Problem problem = problems[i];
                    NotifyProgress(problem.Filename, i, problems.Count);

                    try
                    {
                        if (problem.HasOrderIssue)
                        {
                            FixOrderProblems(problem.RootBlock);
                            string newContent = problem.RootBlock.Generate();
                            BackUpFile(problem.Filename);
                            SaveContent(problem.Filename, newContent);
                            CheckAndNotifyFile(problem.Filename);
                        }
                        else if (problem.HasXamlStrings)
                        {
                            FixXamlFile(problem.Filename);

                            if (!CheckAndNotifyXamlFile(problem.Filename))
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SendToQuarantine(problem.Filename, ex);
                    }
                }

                proccessedFileCount = problems.Count;

                #endregion
            }
            else
            {
                #region Get files list

                IList<string> filesNames = null;
                IList<string> xamlFiles = null;

                if (argumentType.Equals(typeof(string[])))
                {
                    var fileList = e.Argument.To<string[]>();

                    filesNames = fileList.Where(p => Regex.IsMatch(p, string.Format("^{0}$", SEARCH_PATTERN_Cs))).ToList();
                    xamlFiles = fileList.Where(p => Regex.IsMatch(p, string.Format("^{0}$", SEARCH_PATTERN_Xaml))).ToList();
                    _CheckQurantine = false;
                }
                else
                {
                    _CheckQurantine = e.Argument.To<bool>();
                    filesNames = GetFileNames(null, SEARCH_PATTERN_Cs);
                    xamlFiles = GetFileNames(null, SEARCH_PATTERN_Xaml);
                }

                int wholeCount = xamlFiles.Count + filesNames.Count;
                proccessedFileCount = wholeCount;

                #endregion

                #region Check XAML Files

                for (int i = 0; i < xamlFiles.Count; i++)
                {
                    string currentFilename = xamlFiles[i];

                    NotifyProgress(currentFilename, i, wholeCount);

                    try
                    {
                        if (!CheckAndNotifyXamlFile(currentFilename))
                        {
                            break;
                        }
                    }
                    catch (CodeCleanerException ex)
                    {
                        NotifyByException(ex);
                    }
                    catch (Exception ex)
                    {
                        SendToQuarantine(currentFilename, ex);
                    }
                }

                #endregion

                #region Checks Code Files

                for (int i = 0; i < filesNames.Count; i++)
                {
                    string currentFilename = filesNames[i];

                    NotifyProgress(currentFilename, i, wholeCount);

                    try
                    {
                        if (!CheckAndNotifyFile(currentFilename))
                        {
                            break;
                        }
                    }
                    catch (CodeCleanerException ex)
                    {
                        NotifyByException(ex);
                    }
                    catch (Exception ex)
                    {
                        SendToQuarantine(currentFilename, ex);
                    }
                }

                #endregion
            }

            e.Result = proccessedFileCount;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage >= 0)
            {
                if (OnProgressChanged.IsNotNull())
                {
                    OnProgressChanged(this, new NewProgressChangedEventArgs(e.UserState.ToWithNull<string>(), e.ProgressPercentage));
                }
            }
            else
            {
                if (OnNewProblem.IsNotNull())
                {
                    OnNewProblem(this, new NewProblemEventArgs(e.UserState.To<Problem>()));
                }
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error.IsNull())
            {
                if (OnProgressComplete.IsNotNull())
                {
                    OnProgressComplete(this, new ProgressCompleteEventArgs(e.Cancelled, (int)e.Result));
                }
            }
            else
            {
                if (OnProgressComplete.IsNotNull())
                {
                    OnProgressComplete(this, new ProgressCompleteEventArgs(e.Error));
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler<NewProblemEventArgs> OnNewProblem;

        public event EventHandler<NewProgressChangedEventArgs> OnProgressChanged;

        public event EventHandler<ProgressCompleteEventArgs> OnProgressComplete;

        #endregion
    }
}
