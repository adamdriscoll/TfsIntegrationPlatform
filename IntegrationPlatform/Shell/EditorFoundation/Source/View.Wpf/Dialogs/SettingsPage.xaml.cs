// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private SettingsViewModel m_settings;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            m_settings.Save();
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Close()
        {
            if (m_settings != null)
            {
                m_settings.ShellViewModel.ClearModalViewModel();
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is SettingsViewModel)
            {
                m_settings = DataContext as SettingsViewModel;
            }
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            int i;
            e.Handled = !int.TryParse(e.Text, out i);
        }
    }

    public class SettingsConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ShellViewModel)
            {
                return new SettingsViewModel(value as ShellViewModel);
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class SettingsViewModel : INotifyPropertyChanged
    {
        public ShellViewModel ShellViewModel;

        public SettingsViewModel(ShellViewModel viewModel)
        {
            ShellViewModel = viewModel;
            RefreshIntervalSeconds = ShellViewModel.RuntimeManager.RefreshIntervalSeconds;
            ShowStartupDialog = Properties.Settings.Default.ShowStartupDialog;
            IsOutputEnabled = ShellViewModel.RuntimeManager.IsOutputEnabled;
            IsAdvancedRulesEnabled = ShellViewModel.IsAdvancedRulesEnabled;
            ProductVersion = Assembly.GetCallingAssembly().GetName().Version;
        }

        public void Save()
        {
            ShellViewModel.RuntimeManager.RefreshIntervalSeconds = RefreshIntervalSeconds;
            Properties.Settings.Default.ShowStartupDialog = ShowStartupDialog;
            ShellViewModel.RuntimeManager.IsOutputEnabled = IsOutputEnabled;
            ShellViewModel.IsAdvancedRulesEnabled = IsAdvancedRulesEnabled;
        }

        public Version ProductVersion { get; private set; }

        public bool ShowStartupDialog { get; set; }

        public bool IsOutputEnabled { get; set; }

        public bool IsAdvancedRulesEnabled { get; set; }

        private int m_refreshIntervalSeconds;
        public int RefreshIntervalSeconds
        {
            get
            {
                return m_refreshIntervalSeconds;
            }
            set
            {
                m_refreshIntervalSeconds = value;
                OnPropertyChanged("RefreshIntervalSeconds");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion
    }
}
