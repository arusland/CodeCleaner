using CodeCleaner.Properties;
using Microsoft.WindowsAPICodePack.Taskbar;
using Orygin.Shared.Minimal.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CodeCleaner
{
    public partial class MainWindow : Window
    {
        #region Constants

        private const string ERROR_MESSAGE_ProjectNotLoaded = "Project isn't loaded.";
        private const string OPERATION_Stop = "Stop";
        private const string OPERATION_Stopping = "Stopping...";
        private const string TEMPLATE_Title = "Code Cleaner v{0}.{1}";
        private const string TITLE_Problems = "Problems";

        #endregion

        #region Ctors

        public MainWindow()
        {
            InitializeComponent();
            Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;

            _CurrentAppTitle = string.Format(TEMPLATE_Title, appVersion.Major, appVersion.Minor);
            Title = _CurrentAppTitle;
            _Parser = new CodeParser();
            statusProgressBar.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Fields

        private List<Problem> _CheckedProblems;
        private CodeCleanerManager _CodeCleaner;
        private CodeSpecification _CodeSpecification;
        private readonly string _CurrentAppTitle;
        private IFileObserverManager _FileObserver;
        private bool _FixChecked;
        private ICodeParser _Parser;
        private DateTime _ProgressStartTime;
        private CodeCleanerProject _Project;
        private bool _RunOnChecked;

        #endregion

        #region Methods

        #region Private

        private void ClearProject()
        {
            _CodeSpecification = null;
            _Project = null;
            //listProblems.Items.Clear();
            gridProblems.Children.Clear();
            gridProblems.RowDefinitions.Clear();
            statusProgressBar.Value = 0;
            statusLabel.Content = string.Empty;

            if (_CodeCleaner.IsNotNull())
            {
                _CodeCleaner.OnNewProblem -= CodeCleaner_OnNewProblem;
                _CodeCleaner.OnProgressChanged -= CodeCleaner_OnProgressChanged;
                _CodeCleaner.OnProgressComplete -= CodeCleaner_OnProgressComplete;
                _CodeCleaner = null;
            }

            _FileObserver.IfPresent(p => p.Save());
        }

        private void EnableControls(bool enable, bool startOperation, bool fixOperation)
        {
            buttonBrowse.IsEnabled = enable;
            buttonRun.IsEnabled = enable || startOperation;
            checkBoxCheckAll.IsEnabled = checkBoxRunChecked.IsEnabled = gridProblems.Children.Count > 0;
            checkBoxCheckAll.IsChecked = checkBoxRunChecked.IsChecked = gridProblems.Children.OfType<ProblemViewControl>().Any(p => p.IsCheked);
            buttonFix.IsEnabled = (enable || fixOperation) && gridProblems.Children.OfType<ProblemViewControl>().Any(p => p.Problem.CanFixProblem);
            buttonTestGenerating.IsEnabled = enable;

            if (!startOperation)
            {
                statusProgressBar.Value = 0;
                //statusLabel.Content = string.Empty;
                statusProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void InitProgress()
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressValue(0, 100);
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            }
        }

        private void OpenProject(string fileName)
        {
            ClearProject();

            try
            {
                _Project = new CodeCleanerProject(fileName);
                _CodeSpecification = new CodeSpecification();

                _CodeSpecification.Load(_Project.CodeSpecificationPath);

                Title = string.Format("{1} - {0}", _Project.FilesSearchPaths.FirstOrDefault(), _CurrentAppTitle);
                textBoxProjectPath.Text = fileName;
                _FileObserver = new FileObserverManager();
                _CodeCleaner = new CodeCleanerManager(_CodeSpecification, _Parser, _Project, _FileObserver);
                _CodeCleaner.OnNewProblem += CodeCleaner_OnNewProblem;
                _CodeCleaner.OnProgressChanged += CodeCleaner_OnProgressChanged;
                _CodeCleaner.OnProgressComplete += CodeCleaner_OnProgressComplete;
                EnableControls(true, false, false);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                ClearProject();
                UpdateAllIssuesCount();
            }
        }

        private void ShowError(string message)
        {
            Activate();
            MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            statusLabel.Content = string.Format("ERROR: {0}", message);
        }

        private void ShowInfo(string message)
        {
            Activate();
            MessageBox.Show(this, message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);

            statusLabel.Content = message;
        }

        private string Timespan2Str(TimeSpan ts)
        {
            StringBuilder timeMsg = new StringBuilder();

            if ((int)ts.TotalMinutes > 0)
            {
                timeMsg.AppendFormat("{0} min {1} sec", (int)ts.TotalMinutes, (int)ts.TotalSeconds % 60);
            }
            else if ((int)ts.Seconds > 0)
            {
                timeMsg.AppendFormat("{0} sec", (int)ts.TotalSeconds);
            }
            else
            {
                timeMsg.AppendFormat("{0} ms", (int)ts.TotalMilliseconds);
            }

            return timeMsg.ToString();
        }

        private void UpdateAllIssuesCount()
        {
            int problemsCount = gridProblems.Children.OfType<ProblemViewControl>().Count();
            int allIssuesCount = gridProblems.Children.OfType<ProblemViewControl>().Select(p => p.Problem).Sum(p => p.Issues.Count);

            if (allIssuesCount > 0)
            {
                tabItemProblems.Header = string.Format("{0} ({1}/{2} issues)", TITLE_Problems, problemsCount, allIssuesCount);
            }
            else
            {
                tabItemProblems.Header = TITLE_Problems;
            }
        }

        #endregion

        #endregion

        #region Event Handlers

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog()
            {
                FileName = textBoxProjectPath.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OpenProject(dialog.FileName);
            }
        }

        private void buttonFix_Click(object sender, RoutedEventArgs e)
        {
            InitProgress();

            if (_CodeCleaner.IsNotNull())
            {
                if (_CodeCleaner.IsWorking)
                {
                    buttonFix.Content = OPERATION_Stopping;
                    EnableControls(false, false, false);
                    _CodeCleaner.StopAsync();
                }
                else
                {
                    _CheckedProblems = GetCheckedProblems().Where(p => p.CanFixProblem).ToList();

                    if (_CheckedProblems.Count > 0)
                    {
                        if (MessageBox.Show("Are you sure to fix checked problems?", "Warning",
                            MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                        {
                            _FixChecked = true;
                            buttonRun.Content = OPERATION_Stop;
                            EnableControls(false, false, true);
                            _CodeCleaner.NormalizeFileAsync(_CheckedProblems.ToList());

                        }
                    }
                    else
                    {
                        ShowError("Not one of selected problem cannot be fixed.");
                    }
                }
            }
            else
            {
                ShowError(ERROR_MESSAGE_ProjectNotLoaded);
            }
        }

        private void buttonRun_Click(object sender, RoutedEventArgs e)
        {
            InitProgress();

            if (_CodeCleaner.IsNotNull())
            {
                if (_CodeCleaner.IsWorking)
                {
                    buttonRun.Content = OPERATION_Stopping;
                    EnableControls(false, false, false);
                    _CodeCleaner.StopAsync();
                }
                else
                {
                    _ProgressStartTime = DateTime.Now;
                    _RunOnChecked = checkBoxRunChecked.IsChecked == true;

                    if (_RunOnChecked)
                    {
                        _CheckedProblems = GetCheckedProblems();

                        string[] checkedFiles = _CheckedProblems.Select(p => p.Filename).ToArray();

                        if (checkedFiles.Length > 0)
                        {
                            buttonRun.Content = OPERATION_Stop;
                            EnableControls(false, true, false);
                            _CodeCleaner.StartAsync(checkedFiles);
                        }
                        else
                        {
                            ShowError("You should check at least one problem.");
                        }
                    }
                    else
                    {
                        buttonRun.Content = OPERATION_Stop;
                        gridProblems.Children.Clear();
                        gridProblems.RowDefinitions.Clear();
                        EnableControls(false, true, false);
                        _CodeCleaner.StartAsync(checkBoxQuarantine.IsChecked == true);
                    }
                }
            }
            else
            {
                ShowError(ERROR_MESSAGE_ProjectNotLoaded);
            }
        }

        private void buttonTestGenerating_Click(object sender, RoutedEventArgs e)
        {
            if (_CodeCleaner.IsNotNull())
            {
                var problemFiles = _CodeCleaner.CheckGeneratingFiles(null);

                if (problemFiles.Length > 0)
                {
                    ShowError(string.Format("Found {0} files that cannot be saved.", problemFiles.Length));
                }
                else
                {
                    ShowInfo("Generating successfully tested.");
                }
            }
            else
            {
                ShowError(ERROR_MESSAGE_ProjectNotLoaded);
            }
        }

        private void checkBoxCheckAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in gridProblems.Children.OfType<ProblemViewControl>())
            {
                checkBox.IsCheked = checkBoxCheckAll.IsChecked == true;
            }
        }

        private void CodeCleaner_OnNewProblem(object sender, NewProblemEventArgs e)
        {
            if (_RunOnChecked || _FixChecked)
            {
                ProblemViewControl item = gridProblems.Children.OfType<ProblemViewControl>().FirstOrDefault(p => p.Problem.Filename == e.Problem.Filename);

                if (item.IsNotNull())
                {
                    item.Problem = e.Problem;                    
                    _CheckedProblems.Remove(_CheckedProblems.First(p => p.Filename == item.Problem.Filename));
                }
                else
                {
                    ShowError("Problem not found - " + e.Problem.Filename);
                }
            }
            else
            {
                ProblemViewControl item = new ProblemViewControl(e.Problem);

                item.OnViewSource += item_OnViewSource;
                RowDefinition rowDef = new RowDefinition();
                gridProblems.RowDefinitions.Add(rowDef);
                gridProblems.Children.Add(item);
                Grid.SetRow(item, gridProblems.RowDefinitions.Count - 1);
            }

            UpdateAllIssuesCount();

            if (checkBoxAutoscroll.IsChecked == true)
            {
                //listProblems.ScrollIntoView(item);
                scrollViewerMain.ScrollToBottom();
            }
        }

        private void CodeCleaner_OnProgressChanged(object sender, NewProgressChangedEventArgs e)
        {
            statusProgressBar.Visibility = e.Percentage > 0 ? Visibility.Visible : Visibility.Collapsed;
            statusProgressBar.Value = e.Percentage;
            statusLabel.Content = e.NextFilename;

            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressValue(e.Percentage, 100);
            }
        }

        private void CodeCleaner_OnProgressComplete(object sender, ProgressCompleteEventArgs e)
        {
            if (e.Error.IsNotNull())
            {
                ShowError(e.Error.Message);
            }
            else if (e.Canceled)
            {
                ShowError("Operation canceled by user.");
            }
            else
            {
                TimeSpan ts = DateTime.Now.Subtract(_ProgressStartTime);
                string message = string.Format("{1} files proccessed  in {0}", Timespan2Str(ts), e.ProccessedFileCount);

                if (this.IsActive)
                {
                    statusLabel.Content = message;
                    //Console.Beep(100, 100);
                }
                else
                {
                    ShowInfo(message);
                    Activate();
                }

                if (_RunOnChecked || _FixChecked)
                {
                    var toDelete = gridProblems.Children.OfType<ProblemViewControl>().Where(p => _CheckedProblems.Any(f => f.Filename == p.Problem.Filename)).ToList();

                    foreach (var control in toDelete)
                    {
                        gridProblems.Children.Remove(control);
                    }
                }
            }

            UpdateAllIssuesCount();
            EnableControls(true, false, true);
            buttonRun.Content = "Start";
            buttonFix.Content = "Fix checked";
            _RunOnChecked = _FixChecked = false;

            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            }
        }

        private List<Problem> GetCheckedProblems()
        {
            return gridProblems.Children.OfType<ProblemViewControl>().Where(p => p.IsCheked).Select(p => p.Problem).ToList();
        }

        private void item_OnViewSource(object sender, EventArgs e)
        {
            try
            {
                ProblemViewControl view = sender.To<ProblemViewControl>();

                System.Diagnostics.Process.Start(view.Problem.Filename);
            }
            catch (System.Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void textBoxProjectPath_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            buttonTestGenerating.Visibility = Visibility.Visible;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            buttonRun.Focus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.LastUsedProjectPath = textBoxProjectPath.Text;
            Settings.Default.Save();

            _FileObserver.IfPresent(p => p.Save());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.LastUsedProjectPath.IsNotNullOrEmpty())
            {
                OpenProject(Settings.Default.LastUsedProjectPath);
            }
            EnableControls(true, false, false);
        }

        #endregion
    }
}
