// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Shell.ViewModel;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for AddinsDialog.xaml
    /// </summary>
    public partial class AddinsDialog : Window
    {
        public AddinsDialog(AddinsDialogViewModel dialogVM)
        {
            InitializeComponent();
            this.DataContext = dialogVM;
            AddinsDialogVM = dialogVM;
        }
        public AddinsDialogViewModel AddinsDialogVM { get; set; }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            AddinsDialogVM.Save();
            DialogResult = true;
            Close();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            AddinsDialogVM.MoveRight();
        }

        private void DeSelectButton_Click(object sender, RoutedEventArgs e)
        {
            AddinsDialogVM.MoveLeft();
        }

        private void AddinListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddinsDialogVM.MoveRight();
        }

        private void SelectedAddinsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddinsDialogVM.MoveLeft();
        }
    }

    public class AddinsDialogViewModel : ViewModelBase
    {
        public AddinsDialogViewModel(MigrationSource source)
        {
            Source = source;
            m_addinConfigurations = new Dictionary<IAddin, Dictionary<string, FieldValuePairViewModel>>();
        }

        public void Save()
        {
            Source.Settings.Addins.Addin.Clear(); 
            foreach (IAddin addin in ConfiguredAddins)
            {
                AddinElement addinElement = new AddinElement();
                addinElement.ReferenceName = addin.ReferenceName.ToString();
                addinElement.FriendlyName = addin.FriendlyName;

                Dictionary<string, FieldValuePairViewModel> fieldVMs;
                m_addinConfigurations.TryGetValue(addin, out fieldVMs);
                if (addin.CustomSettingKeys != null)
                {
                    foreach (string str in addin.CustomSettingKeys)
                    {
                        FieldValuePairViewModel currentVM;
                        fieldVMs.TryGetValue(str, out currentVM);
                        if (currentVM != null)
                        {
                            CustomSetting customSetting = new CustomSetting();
                            customSetting.SettingKey = str;
                            customSetting.SettingValue = currentVM.FieldValue;
                            addinElement.CustomSettings.CustomSetting.Add(customSetting);
                        }
                    }
                }

                Source.Settings.Addins.Addin.Add(addinElement);
            }
        }
        public MigrationSource Source { get; set; }

        public ObservableCollection<IAddin> Addins
        {
            get
            {
                if (m_addins == null)
                {
                    m_addins = new ObservableCollection<IAddin>();
                    LoadAddins();
                    FindConfiguredAddins();
                }

                return m_addins;
            }
        }

        private void LoadAddins()
        {
            IEnumerable<ProviderHandler> providers = Microsoft.TeamFoundation.Migration.Toolkit.Utility.LoadProvider(new DirectoryInfo(Constants.PluginsFolderName));

            // Initialize a list that will contain all plugin types discovered
            Dictionary<Guid, ProviderHandler> providerHandlers = new Dictionary<Guid, ProviderHandler>();

            foreach (ProviderHandler handler in providers)
            {
                try
                {
                    IProvider provider = handler.Provider;
                    IAddin addIn = provider.GetService(typeof(IAddin)) as IAddin;
                    if (null != addIn)
                    {
                        if (addIn.SupportedMigrationProviderNames == null)
                        {
                            m_addins.Add(addIn);
                        }
                        else
                        {
                            foreach (Guid supportedProvider in addIn.SupportedMigrationProviderNames)
                            {
                                if (Source.ProviderReferenceName.Equals(supportedProvider.ToString()))
                                {
                                    m_addins.Add(addIn);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utilities.DefaultTraceSource.TraceEvent(TraceEventType.Error, 0, "A failure occurred while trying to load the {0} Provider: {1}{2}", handler.ProviderName, Environment.NewLine, ex.ToString());
                }
            }
        }

        private void FindConfiguredAddins()
        {
            Dictionary<IAddin, AddinElement> addinsAndAddinElementsToMove = new Dictionary<IAddin, AddinElement>();

            foreach(IAddin addin in Addins)
            {
                foreach (AddinElement addinElement in Source.Settings.Addins.Addin)
                {
                    if (string.Equals(addin.ReferenceName.ToString(), addinElement.ReferenceName, StringComparison.OrdinalIgnoreCase))
                    {
                        addinsAndAddinElementsToMove.Add(addin, addinElement);
                        break;
                    }
                }
            }

            foreach (KeyValuePair<IAddin, AddinElement> addinElementPair in addinsAndAddinElementsToMove)
            {
                Dictionary<string, FieldValuePairViewModel> fieldVMs;
                SelectedAddin = addinElementPair.Key;
                MoveRight();
                ConfiguredAddin = addinElementPair.Key;
                
                m_addinConfigurations.TryGetValue(addinElementPair.Key, out fieldVMs);
                if (addinElementPair.Key.CustomSettingKeys != null)
                {
                    foreach (string str in addinElementPair.Key.CustomSettingKeys)
                    {
                        FieldValuePairViewModel currentVM;
                        fieldVMs.TryGetValue(str, out currentVM);
                        if (currentVM != null)
                        {
                            foreach (CustomSetting customSetting in addinElementPair.Value.CustomSettings.CustomSetting)
                            {
                                if (string.Equals(str, customSetting.SettingKey, StringComparison.OrdinalIgnoreCase))
                                {
                                    currentVM.FieldValue = customSetting.SettingValue;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private ObservableCollection<IAddin> m_addins;

        public void MoveRight()
        {
            if (SelectedAddin != null)
            {
                ConfiguredAddins.Add(SelectedAddin);
                Addins.Remove(SelectedAddin);
            }
        }

        public void MoveLeft()
        {
            if (ConfiguredAddin != null)
            {
                Addins.Add(ConfiguredAddin);
                m_addinConfigurations.Remove(ConfiguredAddin); 
                ConfiguredAddins.Remove(ConfiguredAddin);
            }
        }

        private ObservableCollection<FieldValuePairViewModel> m_fieldValues;
        public ObservableCollection<FieldValuePairViewModel> FieldValues
        {
            get
            {
                if (m_fieldValues == null)
                {
                    m_fieldValues = new ObservableCollection<FieldValuePairViewModel>();
                }
                return m_fieldValues;
            }
        }

        private bool m_showError;
        public bool ShowError
        {
            get
            {
                return m_showError;
            }
            set
            {
                if (m_showError != value)
                {
                    m_showError = value;
                    OnPropertyChanged("ShowError");
                }
            }
        }

        private IAddin m_selectedAddin;
        public IAddin SelectedAddin
        {
            get
            {
                return m_selectedAddin;
            }
            set
            {
                if (m_selectedAddin != value)
                {
                    m_selectedAddin = value;
                    OnPropertyChanged("SelectedAddin");
                }
            }
        }
        private IAddin m_configuredAddin;
        public IAddin ConfiguredAddin
        {
            get
            {
                return m_configuredAddin;
            }
            set
            {
                if (m_configuredAddin != value)
                {
                    m_configuredAddin = value;
                    FieldValues.Clear();
                    if (m_configuredAddin != null)
                    {
                        if (m_configuredAddin.CustomSettingKeys == null || m_configuredAddin.CustomSettingKeys.Count == 0)
                        {
                            ShowError = true;
                        }
                        else
                        {
                            ShowError = false;
                            if (m_addinConfigurations.ContainsKey(m_configuredAddin))
                            {
                                Dictionary<string, FieldValuePairViewModel> settingVMs = new Dictionary<string, FieldValuePairViewModel>();
                                m_addinConfigurations.TryGetValue(m_configuredAddin, out settingVMs);
                                if (settingVMs != null)
                                {
                                    foreach (FieldValuePairViewModel fieldValue in settingVMs.Values)
                                    {
                                        FieldValues.Add(fieldValue);
                                    }
                                }
                            }
                            else
                            {
                                Dictionary<string, FieldValuePairViewModel> settingVMs = new Dictionary<string, FieldValuePairViewModel>();
                                foreach (string str in m_configuredAddin.CustomSettingKeys)
                                {
                                    FieldValuePairViewModel fieldVM = new FieldValuePairViewModel();
                                    fieldVM.FieldHeading = str;
                                    settingVMs.Add(str, fieldVM);
                                    FieldValues.Add(fieldVM);
                                }
                                m_addinConfigurations.Add(m_configuredAddin, settingVMs);
                            }
                        }
                    }
                    else
                    {
                        FieldValues.Clear();
                    }
                    OnPropertyChanged("ConfiguredAddin");
                }
            }
        }

        private Dictionary<IAddin, Dictionary<string, FieldValuePairViewModel>> m_addinConfigurations;

        public ObservableCollection<IAddin> ConfiguredAddins
        {
            get
            {
                if (m_configuredAddins == null)
                {
                    m_configuredAddins = new ObservableCollection<IAddin>();
                }

                return m_configuredAddins;
            }
        }
        private ObservableCollection<IAddin> m_configuredAddins;

    }
    public class FieldValuePairViewModel : ViewModelBase
    {
        private string m_fieldHeading;
        public string FieldHeading
        {
            get
            {
                return m_fieldHeading;
            }
            set
            {
                m_fieldHeading = value;
                OnPropertyChanged("FieldHeading");
            }
        }
        private string m_fieldValue;
        public string FieldValue
        {
            get
            {
                return m_fieldValue;
            }
            set
            {
                m_fieldValue = value;
                OnPropertyChanged("FieldValue");
            }
        }
    }

}
