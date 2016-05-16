// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Windows;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.AdminUtilities.ServiceAccount;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // attach global exception handler
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionEventHandler);
            
            // check for existing process
            Process thisProcess = Process.GetCurrentProcess();
            Process[] runningProcesses = Process.GetProcessesByName(thisProcess.ProcessName);
            if (runningProcesses.Length > 1)
            {
                Process existingProcess = runningProcesses.FirstOrDefault(x => x.Id != thisProcess.Id);
                Debug.Assert(existingProcess != null);
                SetForegroundWindow(existingProcess.MainWindowHandle);
                Application.Current.Shutdown();
                return;
            }
            try
            {
                using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
                {
                    var v = context.RTSessionGroupConfigSet.Count();
                }
            }
            catch (EntityException ex)
            {
                Exception exception = ex.InnerException ?? ex;
                Utilities.HandleException(exception, true, "Connection Error", "Could not connect to database.  Please check (1) connection string in MigrationToolServers.config and (2) database permissions.");
                Application.Current.Shutdown();
                return;
            }

            // check for running service
            if (GlobalConfiguration.UseWindowsService)
            {
                System.ServiceProcess.ServiceController service = new System.ServiceProcess.ServiceController(Constants.TfsIntegrationServiceName);
                try
                {
                    if (service.Status != System.ServiceProcess.ServiceControllerStatus.Running)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendFormat("The TFS Integration Service is not running.  The current status is {0}.  ", service.Status);
                        stringBuilder.AppendLine("Please do one of the following:");
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine("1. Start the service using Windows Services.");
                        stringBuilder.AppendLine("2. Use setup in Control Panel to Change this application and uninstall the TFS Integration Service feature.");
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine("This application will now close.");

                        MessageBox.Show(stringBuilder.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                        return;
                    }
                }
                catch (InvalidOperationException)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine("The TFS Integration Service is not installed, but the Shell is configured to use the service.  Please do one of the following:");
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("1. Use setup in Control Panel to Change or Repair this application.");
                    stringBuilder.AppendLine("2. Set UseWindowsService in MigrationToolServers.config to false.");
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine("This application will now close.");

                    MessageBox.Show(stringBuilder.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
            }

            // check permissions
            AccountValidationResult result = ServiceAccountUtility.CurrentAccountHasAllServiceAccountPermissions(ServiceType.TfsIntegrationService);
            result &= ~AccountValidationResult.NotTfsIntegrationServiceLogonAccount; // don't care if account does not have service permissions
            if ((result & AccountValidationResult.ValidationFailed) != 0)
            {
                MessageBox.Show("Failed to validate permissions.  Please restart application with the appropriate permissions.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
            else if (result != AccountValidationResult.Valid)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("You are missing the following permissions recommended to run this application:");
                stringBuilder.AppendLine();
                if ((result & AccountValidationResult.NotInTFSIPEXECRole) != 0)
                {
                    stringBuilder.AppendLine("Membership in TFSIP EXEC database role");
                }
                if ((result & AccountValidationResult.NotInTFSIPEXECWorkProcessGroup) != 0)
                {
                    stringBuilder.AppendLine("Membership in local TFSIP Worker Process Group");
                }
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("Do you wish to attempt to add yourself as a member? Click No to skip and attempt to run with current permissions.");

                MessageBoxResult messageBoxResult = MessageBox.Show(stringBuilder.ToString(), "Group Membership Test", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    GrantPermissions(ref result);
                    if (result != AccountValidationResult.Valid)
                    {
                        MessageBox.Show("Unable to change membership.  Please run this application as a local administrator.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    // continue
                }
            }

            base.OnStartup(e);
        }

        private AccountValidationResult GrantPermissions(ref AccountValidationResult result)
        {
            string account = WindowsIdentity.GetCurrent().Name;

            if ((result & AccountValidationResult.NotInTFSIPEXECRole) != 0)
            {
                try
                {
                    var rslt = DBRoleUtil.CreateWindowsLogin(account);
                    if (rslt != DBRoleUtil.AccountsResult.Fail)
                    {
                        rslt = DBRoleUtil.CreateTFSIPEXECRole();
                    }
                    if (rslt != DBRoleUtil.AccountsResult.Fail)
                    {
                        rslt = DBRoleUtil.AddAccountToTFSIPEXECRole(account);
                    }
                    if (rslt != DBRoleUtil.AccountsResult.Fail)
                    {
                        result &= ~AccountValidationResult.NotInTFSIPEXECRole;
                    }
                }
                catch { }
            }
            if ((result & AccountValidationResult.NotInTFSIPEXECWorkProcessGroup) != 0)
            {
                try
                {
                    WindowsGroupUtil.CreateGroup(Constants.TfsIntegrationExecWorkProcessGroupName, Constants.TfsIntegrationExecWorkProcessGroupComment);
                    WindowsGroupUtil.AddMemberToGroup(Constants.TfsIntegrationExecWorkProcessGroupName, account);
                    result &= ~AccountValidationResult.NotInTFSIPEXECWorkProcessGroup;
                }
                catch { }
            }
            return result;
        }

        private static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Utilities.HandleException(e.ExceptionObject as Exception, true, ShellResources.Caption_Error, ShellResources.UnhandledException);
            Environment.Exit(0);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
