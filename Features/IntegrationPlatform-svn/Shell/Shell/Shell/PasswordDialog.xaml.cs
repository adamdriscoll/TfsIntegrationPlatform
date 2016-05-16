// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Security;
using System.Windows;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Interaction logic for PasswordDialog.xaml
    /// </summary>
    public partial class PasswordDialog : Window
    {
        public PasswordDialog(string account)
        {
            InitializeComponent();
            accountField.Text = account;
            passwordField.Focus();
        }

        public SecureString SecurePassword { get; private set; }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.Equals(passwordField.Password, confirmPasswordField.Password))
            {
                // passwords don't match
                MessageBox.Show("The passwords you entered did not match.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                confirmPasswordField.Password = string.Empty;
                confirmPasswordField.Focus();
            }
            else
            {
                SecurePassword = passwordField.SecurePassword;
                DialogResult = true;
                Close();
            }
        }
    }
}
