// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.TeamFoundation.Migration.Shell.ConflictManagement;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.TfsCommonShellAdapter
{
    /// <summary>
    /// Interaction logic for BasicConflictTypeCustomControl.xaml
    /// </summary>
    public partial class BasicConflictTypeCustomControl : UserControl
    {
        public BasicConflictTypeCustomControl()
        {
            InitializeComponent();
        }
    }

    public abstract class ConflictTypeViewModelBase : IConflictTypeUserControl
    {
        public string ConflictTypeDescription { get; set; }

        protected ConflictRuleViewModel m_viewModel;

        private List<ResolutionActionViewModel> m_resolutionActions = new List<ResolutionActionViewModel>();

        public List<ResolutionActionViewModel> ResolutionActions
        {
            get
            {
                return m_resolutionActions;
            }
        }

        private ResolutionActionViewModel SelectedResolutionAction
        {
            get
            {
                return ResolutionActions.Single(x => x.IsSelected);
            }
        }

        public void RegisterResolutionAction(ResolutionActionViewModel resolutionAction)
        {
            m_resolutionActions.Add(resolutionAction);
        }

        public virtual void Execute()
        {
            m_viewModel.SelectedResolutionAction = m_viewModel.ResolutionActions.First(x => x.ReferenceName.Equals(SelectedResolutionAction.ResolutionActionReferenceName));
            m_viewModel.Description = SelectedResolutionAction.ResolutionActionDescription;
            if (SelectedResolutionAction.ExecuteCommand != null)
            {
                SelectedResolutionAction.ExecuteCommand();
            }
        }

        #region IConflictTypeUserControl Members

        private UserControl m_userControl;
        public UserControl UserControl
        {
            get
            {
                if (m_userControl == null)
                {
                    m_userControl = new BasicConflictTypeCustomControl();
                }
                return m_userControl;
            }
        }

        private Dictionary<string, FrameworkElement> m_details;
        public virtual Dictionary<string, FrameworkElement> Details
        {
            get
            {
                if (m_details == null)
                {
                    m_details = new Dictionary<string, FrameworkElement>();

                    string defaultDetails;
                    ConflictDetailsProperties properties = m_viewModel.MigrationConflict.ConflictDetailsProperties;
                    if (!properties.TryGetValue(ConflictDetailsProperties.DefaultConflictDetailsKey, out defaultDetails))
                    {
                        defaultDetails = m_viewModel.ConflictDetails;
                    }

                    m_details["Details"] = CreateTextElement(m_viewModel.ConflictDetails);
                }
                return m_details;
            }
        }

        public void Save()
        {
            Execute();
        }

        public virtual void SetConflictRuleViewModel(ConflictRuleViewModel viewModel)
        {
            m_viewModel = viewModel;
            UserControl.DataContext = this;
        }

        public static TextBox CreateTextElement(string str)
        {
            TextBox textBox = new TextBox();
            textBox.MaxHeight = 34;
            textBox.ToolTip = str;
            textBox.Text = str;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.Background = Brushes.Transparent;
            textBox.BorderThickness = new Thickness(0);
            textBox.IsReadOnly = true;
            return textBox;
        }

        #endregion
    }

    public delegate void ExecuteResolutionAction();

    public class ResolutionActionViewModel
    {
        public string ResolutionActionDescription { get; set; }

        public Guid ResolutionActionReferenceName { get; set; }

        public bool IsSelected { get; set; }

        public ExecuteResolutionAction ExecuteCommand { get; set; }

        public UserControl UserControl { get; set; }
    }
}
