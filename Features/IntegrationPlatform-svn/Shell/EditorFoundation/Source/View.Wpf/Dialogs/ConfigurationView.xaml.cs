// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for ConfigurationView.xaml
    /// </summary>
    public partial class ConfigurationView : UserControl
    {
        private ConfigurationViewModel m_configuration;

        public ConfigurationView()
        {
            InitializeComponent();
        }

        private void addFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SerializableSession session = (sender as Button).DataContext as SerializableSession;
            m_configuration.AddFilterPair(session);
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ConfigurationViewModel)
            {
                m_configuration = DataContext as ConfigurationViewModel;
                UpdateActiveProviders();
            }
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            m_configuration.IsEditingXml = false;
            m_configuration.ShellViewModel.PopViewModel(m_configuration);
            if (m_configuration.ShellViewModel.SystemState == SystemState.EditConfiguration)
            {
                m_configuration.ShellViewModel.SystemState = SystemState.ConfigurationSaved;
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            m_configuration.IsEditingXml = false;

            ConfigurationViewModel vm = this.DataContext as ConfigurationViewModel;
            if (vm.Cancel())
            {
                vm.ShellViewModel.PopViewModel(m_configuration);
            }
        }

        private void UpdateActiveProviders()
        {
            ICollection<ProviderElement> providers = m_configuration.Providers;
            
            bool providersSynced = m_configuration.MigrationSources.Select(x => x.ProviderReferenceName).Where(x => x != null).Distinct().SequenceEqual(providers.Select(x => x.ReferenceName));
            if (!providersSynced)
            {
                providers.Clear();

                foreach (var source in m_configuration.MigrationSources)
                {
                    if (source.ProviderReferenceName != null)
                    {
                        ProviderHandler providerView = m_configuration.AllProviders.FirstOrDefault(x => x.ProviderId.Equals(new Guid(source.ProviderReferenceName)));
                        if (providerView != null && providers.Count(x => providerView.ProviderId.Equals(new Guid(x.ReferenceName))) == 0)
                        {
                            ProviderElement provider = new ProviderElement();
                            provider.FriendlyName = providerView.ProviderName;
                            provider.ReferenceName = providerView.ProviderId.ToString();
                            providers.Add(provider);
                        }
                    }
                }
            }

            m_configuration.Validate();

            foreach (var session in m_configuration.SerializableSessions)
            {
                if (session.DefaultFilterPair != null)
                {
                    session.DefaultFilterPair.UpdateFilterStringExtensions();
                }
                foreach (var filter in session.FilterPairs)
                {
                    filter.UpdateFilterStringExtensions();
                }
            }

            m_configuration.RefreshFilterStrings();
        }

        private void configurationFriendlyNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_configuration.Model.SessionGroup.FriendlyName = m_configuration.Model.FriendlyName;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SerializableSource serializableSource = (sender as FrameworkElement).DataContext as SerializableSource;
            ProviderHandler providerView = (e.OriginalSource as FrameworkElement).DataContext as ProviderHandler;

            if (serializableSource != null && providerView != null)
            {
                if (m_configuration.Providers.Count(x => providerView.ProviderId.Equals(new Guid(x.ReferenceName))) == 0)
                {
                    ProviderElement provider = new ProviderElement();
                    provider.FriendlyName = providerView.ProviderName;
                    provider.ReferenceName = providerView.ProviderId.ToString();
                    m_configuration.Providers.Add(provider);
                }

                MigrationSource migrationSource = serializableSource.Model;
                string cachedProviderId = migrationSource.ProviderReferenceName;

                IMigrationSourceView serverView = m_configuration.ExtensibilityViewModel.GetMigrationSourceView(providerView.ProviderDescriptionAttribute.ShellAdapterIdentifier);
                serverView.Command(migrationSource);

                if (migrationSource.ProviderReferenceName == null) // user cancelled
                {
                    migrationSource.ProviderReferenceName = cachedProviderId;
                }
                else
                {
                    migrationSource.ProviderReferenceName = providerView.ProviderId.ToString();
                }

                serializableSource.Refresh();

                var session = m_configuration.SerializableSessions.FirstOrDefault(x => x.LeftMigrationSource.Model == migrationSource || x.RightMigrationSource.Model == migrationSource);
                if (session != null)
                {
                    foreach (FilterPair filterPair in session.Model.Filters.FilterPair)
                    {
                        foreach (FilterItem filterItem in filterPair.FilterItem)
                        {
                            if (string.Equals(filterItem.MigrationSourceUniqueId, migrationSource.InternalUniqueId, StringComparison.OrdinalIgnoreCase))
                            {
                                filterItem.FilterString = filterItem.FilterString.Replace("<SourceIdentifier>", migrationSource.SourceIdentifier);
                            }
                        }
                    }
                }
                UpdateActiveProviders();
            }
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            SerializableElement element = textBox.DataContext as SerializableElement;
            if (element != null)
            {
                if (textBox.LineCount > 0)
                {
                    element.LineNumber = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart) + 1;
                    element.ColumnNumber = textBox.SelectionStart - textBox.GetCharacterIndexFromLineIndex(element.LineNumber - 1) + 1;
                }
                else
                {
                    element.LineNumber = 1;
                    element.ColumnNumber = 1;
                }
            }
        }

        private void deleteFilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterPairViewModel filterPair = (sender as FrameworkElement).DataContext as FilterPairViewModel;
            m_configuration.RemoveFilterPair(filterPair.FilterPair);
        }

        private void addinsButton_Click(object sender, RoutedEventArgs e)
        {
            SerializableSource serializableSource = (sender as FrameworkElement).DataContext as SerializableSource;

            if (serializableSource != null)
            {
                MigrationSource migrationSource = serializableSource.Model;

                AddinsDialogViewModel addinsDialogVM = new AddinsDialogViewModel(migrationSource);
                AddinsDialog dialog = new AddinsDialog(addinsDialogVM);
                dialog.Owner = Application.Current.MainWindow;
                dialog.ShowDialog();
            }
        }

        private void editXmlButton_Click(object sender, RoutedEventArgs e)
        {
            SerializableElement element = (sender as Button).DataContext as SerializableElement;
            element.IsEditingXml = true;

            string cachedSerialization = element.SerializedContent;

            EditXmlDialog dialog = new EditXmlDialog(element);
            dialog.Owner = Window.GetWindow(this);
            bool result = (bool)dialog.ShowDialog();

            if (!result)
            {
                element.SerializedContent = cachedSerialization;
            }

            element.IsEditingXml = false;
        }

        private void statusToggleButton_LostFocus(object sender, RoutedEventArgs e)
        {
            if (xmlEditorTextBox.IsVisible)
            {
                xmlEditorTextBox.Focus();
            }
        }

        private void xmlEditorTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                m_configuration.Save();
                m_configuration.RefreshFilterStrings();
            }
            catch (Exception) // don't care if save is unsuccessful
            {
            }
        }

        private void leftFilterStringButton_Click(object sender, RoutedEventArgs e)
        {
            FilterPairViewModel filterPair = (sender as FrameworkElement).DataContext as FilterPairViewModel;
            Debug.Assert(filterPair.LeftFilterStringExtension != null);
            if (filterPair.LeftFilterStringExtension != null && filterPair.LeftMigrationSource != null)
            {
                filterPair.LeftFilterStringExtension.Command(filterPair.LeftFilterItem, filterPair.LeftMigrationSource);
            }
        }

        private void rightFilterStringButton_Click(object sender, RoutedEventArgs e)
        {
            FilterPairViewModel filterPair = (sender as FrameworkElement).DataContext as FilterPairViewModel;
            Debug.Assert(filterPair.RightFilterStringExtension != null);
            if (filterPair.RightFilterStringExtension != null && filterPair.RightMigrationSource != null)
            {
                filterPair.RightFilterStringExtension.Command(filterPair.RightFilterItem, filterPair.RightMigrationSource);
            }
        }

        private void workFlowScenariosComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_configuration.RefreshFilterStrings();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            string navigateUri = e.Uri.AbsoluteUri;
            // if the URI somehow came from an untrusted source, make sure to
            // validate it before calling Process.Start(), e.g. check to see
            // the scheme is HTTP, etc.
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
    }

    public class ProviderNameConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values[0] == null)
            {
                return null;
            }
            else
            {
                Guid providerId = new Guid(values[0].ToString());
                IEnumerable<ProviderHandler> providers = values[1] as IEnumerable<ProviderHandler>;
                if (providers == null)
                {
                    return null;
                }
                ProviderHandler selectedProvider = providers.FirstOrDefault(x => x.ProviderId.Equals(providerId));
                if (selectedProvider == null)
                {
                    return null;
                }
                else
                {
                    return selectedProvider.ProviderName;
                }
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class ProviderEndpointConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SerializableSource serializableSource = values[0] as SerializableSource;
            IEnumerable<ProviderHandler> allProviders = values[1] as IEnumerable<ProviderHandler>;
            if (allProviders == null)
            {
                return null;
            }
            List<ProviderHandler> filteredProviders = allProviders.Where(x => x.ProviderCapabilityAttribute.SessionType == serializableSource.Session.SessionType).ToList();
            if (filteredProviders.Count(x => string.Equals(x.ProviderCapabilityAttribute.EndpointSystemName, serializableSource.Model.EndpointSystemName)) == 0)
            {
                return filteredProviders;
            }
            else
            {
                return filteredProviders.Where(x => string.Equals(x.ProviderCapabilityAttribute.EndpointSystemName, serializableSource.Model.EndpointSystemName));
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class WorkFlowTypeConverter : IValueConverter
    {
        public static readonly string CustomString = "Custom";
        private static Dictionary<string, WorkFlowType> m_workFlowScenarios;
        
        static WorkFlowTypeConverter()
        {
            m_workFlowScenarios = new Dictionary<string, WorkFlowType>();

            m_workFlowScenarios["One-way migration"] = new WorkFlowType();
            m_workFlowScenarios["One-way migration"].Frequency = Frequency.ContinuousManual;
            m_workFlowScenarios["One-way migration"].DirectionOfFlow = DirectionOfFlow.Unidirectional;
            m_workFlowScenarios["One-way migration"].SyncContext = SyncContext.Disabled;

            m_workFlowScenarios["Two-way sync"] = new WorkFlowType();
            m_workFlowScenarios["Two-way sync"].Frequency = Frequency.ContinuousAutomatic;
            m_workFlowScenarios["Two-way sync"].DirectionOfFlow = DirectionOfFlow.Bidirectional;
            m_workFlowScenarios["Two-way sync"].SyncContext = SyncContext.Disabled;

            m_workFlowScenarios[CustomString] = new WorkFlowType();
        }

        public static IEnumerable<string> WorkFlowScenarioNames
        {
            get
            {
                return m_workFlowScenarios.Keys;
            }
        }
        
        #region IValueConverter Members

        /// <summary>
        /// Convert WorkFlowType to string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            WorkFlowType workFlowType = value as WorkFlowType;
            string name = m_workFlowScenarios.FirstOrDefault(x => x.Value.Frequency == workFlowType.Frequency && x.Value.DirectionOfFlow == workFlowType.DirectionOfFlow && x.Value.SyncContext == workFlowType.SyncContext).Key;
 
            if (name != null)
            {
                return name;
            }
            else
            {
                m_workFlowScenarios[CustomString] = workFlowType;
                return CustomString;
            }
        }

        /// <summary>
        /// Convert string to WorkFlowType
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return m_workFlowScenarios[value as string];
        }

        #endregion
    }
}
