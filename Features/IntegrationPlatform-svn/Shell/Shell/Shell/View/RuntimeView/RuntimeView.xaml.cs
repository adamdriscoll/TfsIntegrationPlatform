// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.Shell.View;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Interaction logic for RuntimeView.xaml
    /// </summary>
    public partial class RuntimeView : UserControl
    {
        private RuntimeManager m_runtimeManager;
        private TextBoxTraceWriter m_writer;

        public RuntimeView()
        {
            InitializeComponent();

            m_writer = new TextBoxTraceWriter(outputTextBox);
        }

        private void outputTextBox_RequestBringIntoView(object sender, System.Windows.RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void outputTextBox_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                if (outputScrollViewer.VerticalOffset + outputScrollViewer.ViewportHeight == outputScrollViewer.ExtentHeight)
                {
                    outputScrollViewer.ScrollToBottom();
                }
            }
        }

        private void clearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            outputTextBox.Text = string.Empty;
        }

        private void UserControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is RuntimeManager)
            {
                if (m_runtimeManager != null)
                {
                    m_runtimeManager.PropertyChanged -= m_runtimeManager_PropertyChanged;
                }
                m_runtimeManager = e.NewValue as RuntimeManager;
                RefreshIsOutputEnabled();
                m_runtimeManager.PropertyChanged += new UndoablePropertyChangedEventHandler(m_runtimeManager_PropertyChanged);
            }
        }

        private void m_runtimeManager_PropertyChanged(ModelObject sender, UndoablePropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName.Equals("IsOutputEnabled"))
            {
                RefreshIsOutputEnabled();
            }
        }

        private void RefreshIsOutputEnabled()
        {
            if (m_runtimeManager.IsOutputEnabled)
            {
                try
                {
                    m_writer.StartListening();
                }
                catch (System.Threading.ThreadStateException) // tried to start already running thread; do nothing
                { }
            }
            else
            {
                m_writer.StopListening();
            }
        }
    }
}
