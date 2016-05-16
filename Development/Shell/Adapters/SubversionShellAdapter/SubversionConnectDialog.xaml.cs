using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.SubversionOM;
using Microsoft.TeamFoundation.Migration.SubversionAdapter.Interop.Subversion.DynamicInvocation;

namespace Microsoft.TeamFoundation.Migration.Shell.SubversionShellAdapter
{
    /// <summary>
    /// Interaction logic for SubversionConnectDialog.xaml
    /// </summary>
    public partial class SubversionConnectDialog : Window
    {
        private SubversionMigrationSourceViewModel m_source;

        public SubversionConnectDialog(MigrationSource source)
        {
            InitializeComponent();
            m_source = new SubversionMigrationSourceViewModel(source);
            DataContext = m_source;
            Closed += new EventHandler(SubversionConnectDialog_Closed);
        }

        void SubversionConnectDialog_Closed(object sender, EventArgs e)
        {
            // Todo, need to dispose subversion connection?
            //m_source.Dispose();
        }

        private void userNameTextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox textBox = sender as TextBox;
                textBox.SelectAll();
            }
        }

        private void validateButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_source.Validate(passwordBox.Password))
            {
                m_source.Save();
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(this, m_source.ValidationMessage, "Subversion Connection", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void passwordBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (sender is PasswordBox)
            {
                PasswordBox textBox = sender as PasswordBox;
                textBox.SelectAll();
            }
        }
    }

    public class SubversionMigrationSourceViewModel : INotifyPropertyChanged
    {
        #region Private Members

        private readonly MigrationSource m_source;
        
        private Repository m_repository;
        private string m_serverUrl;
        private string m_userName;
        private string m_password;
        
        private bool m_isValid = true;
        private string m_validationMessage;
        
        #endregion

        #region Constructor

        public SubversionMigrationSourceViewModel(MigrationSource source)
        {
            m_source = source;
        }

        #endregion

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the subversion server uri
        /// </summary>
        public string ServerUrl
        {
            get
            {
                return m_serverUrl;
            }
            set
            {
                if (!string.Equals(m_serverUrl, value))
                {
                    m_serverUrl = value;
                    OnPropertyChanged("ServerUrl");
                }
            }
        }

        /// <summary>
        /// Gets or sets the user name that is used to connect to subversion
        /// </summary>
        public string UserName
        {
            get
            {
                return m_userName;
            }
            set
            {
                if (!string.Equals(m_userName, value))
                {
                    m_userName = value;
                    OnPropertyChanged("UserName");
                }
            }
        }

        /// <summary>
        /// Gets or sets the password that is used to connect to subversion
        /// </summary>
        public string Password
        {
            get
            {
                return m_password;
            }
            set
            {
                if (!string.Equals(m_password, value))
                {
                    m_password = value;
                    OnPropertyChanged("Password");
                }
            }
        }

        /// <summary>
        /// Gets or sets a result message of the validation process
        /// </summary>
        public string ValidationMessage
        {
            get
            {
                return m_validationMessage;
            }
            set
            {
                if (!string.Equals(m_validationMessage, value))
                {
                    m_validationMessage = value;
                    OnPropertyChanged("ValidationMessage");
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the provided values are valid and can be used to connect to subversion
        /// </summary>
        public bool IsValid
        {
            get
            {
                return m_isValid;
            }
            set
            {
                if (m_isValid != value)
                {
                    m_isValid = value;
                    OnPropertyChanged("IsValid");
                }
            }
        }

        #endregion

        #region Methods

        internal void Save()
        {
            m_source.ServerUrl = ServerUrl;
            m_source.SourceIdentifier = "/";
            m_source.FriendlyName = ServerUrl;
            m_source.ServerIdentifier = ServerUrl;
            SaveCustomSetting("UserName", m_userName);
            SaveCustomSetting("Password", m_password);
        }

        internal bool Validate(string password)
        {
            try
            {
                Password = password;
                var serverUri = new Uri(ServerUrl);
                m_repository = Repository.GetRepository(serverUri, UserName, Password);
                m_repository.EnsureAuthenticated();
                
                IsValid = true;
                ValidationMessage = null;
            }
            catch(UriFormatException)
            {
                ValidationMessage = string.Format(Properties.Resources.Culture, Properties.Resources.InvalidUriFormat, ServerUrl);
                IsValid = false;
            }
            catch (SubversionNotFoundException svnex)
            {
                ValidationMessage = svnex.Message;
                IsValid = false;
            }
            catch (UnauthorizedAccessException)
            {
                ValidationMessage = string.Format(Properties.Resources.Culture, Properties.Resources.UnauthorizedException, ServerUrl);
                IsValid = false;
            }
            catch (Exception ex)
            {
                ValidationMessage = string.Format(Properties.Resources.Culture, Properties.Resources.UnknownErrorWhileConnectingToSVN, ServerUrl, ex.Message);
                IsValid = false;
            }

            return IsValid;
        }

        /// <summary>
        /// Save key-value pair to CustomSettings.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>Item already exists.</returns>
        private bool SaveCustomSetting(string key, string value)
        {
            CustomSetting userNameSetting = m_source.CustomSettings.CustomSetting.FirstOrDefault(x => string.Equals(x.SettingKey, key));
            if (userNameSetting != null)
            {
                userNameSetting.SettingValue = value;
                return true;
            }
            else
            {
                CustomSetting newUserNameSetting = new CustomSetting();
                newUserNameSetting.SettingKey = key;
                newUserNameSetting.SettingValue = value;
                m_source.CustomSettings.CustomSetting.Add(newUserNameSetting);
                return false;
            }
        }

        #endregion
    }
}
