using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Orygin.Shared.Minimal.Extensions;
using Orygin.Shared.Minimal.Helpers;
using CodeCleaner;
using System.Text;

namespace CodeCleaner
{
    public partial class ProblemViewControl : UserControl
    {
        #region Ctors

        public ProblemViewControl(Problem problem)
        {
            Checker.NotNull(problem, "problem");

            InitializeComponent();

            Problem = problem;
            IsCheked = true;
            RootLayout.Background = new SolidColorBrush(Color.FromRgb(232, 232, 255));
            _OpenedBrush = new SolidColorBrush(Color.FromRgb(209, 237, 209));
            _ClosedBrush = RootLayout.Background; //new SolidColorBrush(Color.FromRgb(197, 197, 197)); 
            _QuarantineBrush = new SolidColorBrush(Colors.OrangeRed);
            _OddBrush = new SolidColorBrush(Color.FromRgb(237, 252, 237));
            
        }
        
        #endregion

        #region Fields

        private readonly Brush _OpenedBrush;
        private readonly Brush _ClosedBrush;
        private readonly Brush _QuarantineBrush;
        private readonly Brush _OddBrush;
        private Problem _Problem;        
        
        #endregion

        #region Properties
        
        #region Public

        public Problem Problem
        {
            get { return _Problem; }
            set 
            {
                Checker.NotNull(value, "value");

                _Problem = value;
                UpdateView();
            }
        }

        public bool Expanded
        {
            get
            {
                return gridList.Visibility == Visibility.Visible;
            }
            set
            {
                gridList.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                RootLayout.Background = value ? _OpenedBrush : _ClosedBrush;
                textBlockMessage.FontWeight = value ? FontWeights.Bold : FontWeights.Normal;
            }
        }

        public bool IsCheked
        {
            get
            {
                return checkBoxUse.IsChecked == true;
            }
            set
            {
                checkBoxUse.IsChecked = value;
            }
        }
        
        #endregion        
        
        #endregion

        #region Methods
        
        #region Private

        private void UpdateView()
        {
            textBlockMessage.Text = string.Format("{0} ({1} issues)", Problem.Filename, Problem.Issues.Count);

            if (Problem.Quarantined)
            {
                textBlockMessage.Foreground = _QuarantineBrush;
            }

            gridList.RowDefinitions.Clear();
            gridList.Children.Clear();
            int rowIndex = 0;
            bool isOdd = true;

            foreach (ProblemIssue pi in Problem.Issues)
            {
                RowDefinition rowDef = new RowDefinition();

                rowDef.Height = new GridLength(21, GridUnitType.Auto);
                rowDef.MinHeight = 25;
                gridList.RowDefinitions.Add(rowDef);
                TextBlock textBlock = new TextBlock();
                textBlock.Background = isOdd ? _OddBrush : Brushes.Transparent;
                isOdd = !isOdd;

                if (pi.Quarantined)
                {
                    if (pi.ParseCause.IsNotNull())
                    {
                        textBlock.Text = string.Format("• EXCEPTION: {1}{0}• {2}", Environment.NewLine, pi.ParseCause.Message, pi.Message);
                    }
                    else
                    {
                        textBlock.Text = string.Format("• {0}", pi.Message);
                    }

                    textBlock.Foreground = _QuarantineBrush;
                    textBlock.TextWrapping = TextWrapping.WrapWithOverflow;
                }
                else
                {
                    if (pi.LineNumber > 0)
                    {
                        textBlock.Text = string.Format(" {0} In line {1}", pi.Message, pi.LineNumber);
                    }
                    else
                    {
                        textBlock.Text = pi.Message;
                    }
                }
                textBlock.Margin = new Thickness(3);
                Grid.SetRow(textBlock, rowIndex++);
                gridList.Children.Add(textBlock);
            }
        }
        
        #endregion
        
        #endregion

        #region Event Handlers

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }        

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Expanded = !Expanded;
        }

        private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (OnViewSource.IsNotNull())
            {
                OnViewSource(this, EventArgs.Empty);
            }
        }
        
        #endregion

        #region Events

        public event EventHandler OnViewSource;
        
        #endregion
    }
}
