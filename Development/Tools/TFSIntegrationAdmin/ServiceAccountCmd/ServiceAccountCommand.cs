// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace TFSIntegrationAdmin.ServiceAccountCmd
{
    internal class ServiceAccountCommand : CommandBase
    {
        const string ServiceSwitchL = "/service:";
        const string ServiceSwitchS = "/s:";
        const string AccountSwitchL = "/account:";
        const string AccountSwitchS = "/c:";

        const string UpdateSwitchL = "/Update";
        const string UpdateSwitchS = "/u";
        const string AddSwitchL = "/add";
        const string AddSwitchS = "/a";

        enum ServiceToUpdate
        {
            None = 0,
            IntegrationService = 0x1,
            JobService = 0x2,
        }

        enum Mode
        {
            Add,
            Update,
        }

        private ServiceToUpdate m_serviceToUpdate;
        private string m_account;
        private string m_password;
        private Mode m_mode;

        public override string CommandName
        {
            get { return "ConfigServiceAccount"; /* do not localize */ }
        }

        public override bool TryParseArgs(string[] cmdSpecificArgs)
        {
            if (cmdSpecificArgs.Length == 1
                && (cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch1, StringComparison.OrdinalIgnoreCase)
                    || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch2, StringComparison.OrdinalIgnoreCase)
                    || cmdSpecificArgs[0].Equals(Constants.CmdHelpSwitch3, StringComparison.OrdinalIgnoreCase)))
            {
                PrintHelp = true;
                return true;
            }
            else
            {
                return TryParseAsUpdateServiceAccount(cmdSpecificArgs);
            }
        }

        private bool TryParseAsUpdateServiceAccount(string[] cmdSpecificArgs)
        {
            bool serviceOptionFound = false;
            bool accountOptionFound = false;
            bool modeIsFound = false;

            m_serviceToUpdate = ServiceToUpdate.IntegrationService; // default to update both services
            foreach (string arg in cmdSpecificArgs)
            {
                if (arg.StartsWith(ServiceSwitchL, StringComparison.OrdinalIgnoreCase))
                {
                    if (serviceOptionFound)
                    {
                        // same switch used twice
                        return false;
                    }

                    if (!TryParseServiceSwitch(arg, ServiceSwitchL, out m_serviceToUpdate))
                    {
                        return false;
                    }
                    else
                    {
                        serviceOptionFound = true;
                    }
                }
                else if (arg.StartsWith(ServiceSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    if (serviceOptionFound)
                    {
                        // same switch used twice
                        return false;
                    }

                    if (!TryParseServiceSwitch(arg, ServiceSwitchS, out m_serviceToUpdate))
                    {
                        return false;
                    }
                    else
                    {
                        serviceOptionFound = true;
                    }
                }
                else if (arg.StartsWith(AccountSwitchL, StringComparison.OrdinalIgnoreCase))
                {
                    if (accountOptionFound)
                    {
                        // same switch used twice
                        return false;
                    }

                    if (!TryParseArg(arg, AccountSwitchL, out m_account))
                    {
                        return false;
                    }
                    else
                    {
                        accountOptionFound = true;
                    }
                }
                else if (arg.StartsWith(AccountSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    if (accountOptionFound)
                    {
                        // same switch used twice
                        return false;
                    }

                    if (!TryParseArg(arg, AccountSwitchS, out m_account))
                    {
                        return false;
                    }
                    else
                    {
                        accountOptionFound = true;
                    }
                }
                else if (arg.StartsWith(UpdateSwitchL, StringComparison.OrdinalIgnoreCase)
                    || arg.StartsWith(UpdateSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    if (modeIsFound)
                    {
                        // same switch used twice
                        return false;
                    }
                    m_mode = Mode.Update;
                    modeIsFound = true;
                }
                else if (arg.StartsWith(AddSwitchL, StringComparison.OrdinalIgnoreCase)
                    || arg.StartsWith(AddSwitchS, StringComparison.OrdinalIgnoreCase))
                {
                    if (modeIsFound)
                    {
                        // same switch used twice
                        return false;
                    }
                    m_mode = Mode.Add;
                    modeIsFound = true;
                }
                else
                {
                    return false;
                }
            }

            if (modeIsFound)
            {
                switch (m_mode)
                {
                    case Mode.Add:
                        return !string.IsNullOrEmpty(m_account);
                    case Mode.Update:
                        return true;
                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override Interfaces.ICommandResult Run()
        {
            if (!Utility.IsRunAsAdministrator())
            {
                return new ServiceAccountRslt(
                    string.Format(ResourceStrings.ErrorNeedAdminPrivilegeToRunCommandFormat, this.CommandName),this);
            }

            m_password = PromptForPassword();
            Console.WriteLine();

            if (m_serviceToUpdate == ServiceToUpdate.JobService)
            {
                return UpdateAccount(ServiceToUpdate.JobService);
            }
            else if (m_serviceToUpdate == ServiceToUpdate.IntegrationService)
            {
                return UpdateAccount(ServiceToUpdate.IntegrationService);
            }
            else
            {
                // we should never reach here
                throw new InvalidOperationException("ServiceToUpdate is not specified");
            }
        }

        public override string GetHelpString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Add a service account:");
            sb.AppendFormat("  {0} {1} {2} [{3}{4}] {5}{6}",
                Program.ProgramName, CommandName, AddSwitchL, ServiceSwitchL, "<IntegrationService|JobService>", AccountSwitchL, "<account>");
            sb.AppendLine();
            sb.AppendFormat("  {0} {1} {2} [{3}{4}] {5}{6}",
                Program.ProgramName, CommandName, AddSwitchS, ServiceSwitchS, "<IntegrationService|JobService>", AccountSwitchS, "<account>");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("Update the password of the existing service account:");
            sb.AppendFormat("  {0} {1} [{2}{3}]",
                Program.ProgramName, CommandName, ServiceSwitchL, "<IntegrationService|JobService>");
            sb.AppendLine();
            sb.AppendFormat("  {0} {1} [{2}{3}]",
                Program.ProgramName, CommandName, ServiceSwitchS, "<IntegrationService|JobService>");
            return sb.ToString();
        }

        private Interfaces.ICommandResult UpdateAccount(ServiceToUpdate serviceToUpdate)
        {
            string windowsServiceName = string.Empty;
            switch (serviceToUpdate)
            {
                case ServiceToUpdate.IntegrationService:
                    windowsServiceName = Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationServiceName;
                    break;
                case ServiceToUpdate.JobService:
                    windowsServiceName = Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationJobServiceName;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            switch (m_mode)
            {
                case Mode.Add:
                    return AddAccount(windowsServiceName);
                case Mode.Update:
                    return UpdatePassword(windowsServiceName);
                default:
                    throw new InvalidOperationException();
            }
        }

        private Interfaces.ICommandResult UpdatePassword(string windowsServiceName)
        {
            try
            {
                WindowsServiceLogonUtil.UpdateWindowsServiceLogonPassword(windowsServiceName, m_password);
                return new ServiceAccountRslt(this);
            }
            catch (Exception e)
            {
                return new ServiceAccountRslt(e.ToString(), this);
            }
        }

        private Interfaces.ICommandResult AddAccount(string windowsServiceName)
        {
            try
            {
                WindowsServiceLogonUtil.ChangeWindowsServiceLogon(windowsServiceName, m_account, m_password);

                WindowsGroupUtil.CreateGroup(Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationExecWorkProcessGroupName,
                    Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationExecWorkProcessGroupComment);

                WindowsGroupUtil.AddMemberToGroup(
                    Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationExecWorkProcessGroupName,
                    m_account);
                var rslt = DBRoleUtil.CreateWindowsLogin(m_account);
                if (rslt == DBRoleUtil.AccountsResult.Fail)
                {
                    return new ServiceAccountRslt(ResourceStrings.ErrorFailToAddAccountToTFSIPEXECRole, this);
                }
                else
                {
                    rslt = DBRoleUtil.CreateTFSIPEXECRole();
                }

                if (rslt == DBRoleUtil.AccountsResult.Fail)
                {
                    return new ServiceAccountRslt(ResourceStrings.ErrorFailToAddAccountToTFSIPEXECRole, this);
                }
                else
                {
                    rslt = DBRoleUtil.AddAccountToTFSIPEXECRole(m_account);
                }

                if (rslt == DBRoleUtil.AccountsResult.Fail)
                {
                    return new ServiceAccountRslt(ResourceStrings.ErrorFailToAddAccountToTFSIPEXECRole, this);
                }
                else
                {
                    return new ServiceAccountRslt(this);
                }
            }
            catch (Exception e)
            {
                return new ServiceAccountRslt(e.ToString(), this);
            }
        }

        private string PromptForPassword()
        {
            if (!string.IsNullOrEmpty(m_account))
            {
                Console.Write(string.Format(
                    "Please enter the password for the new service account ({0}): ", m_account));
            }
            else
            {
                Console.Write("Please enter the new password: ");
            }

            return ReadMaskedInput();
        }

        private string ReadMaskedInput()
        {
            Stack<string> passbits = new Stack<string>();

            for (ConsoleKeyInfo cki = Console.ReadKey(true); cki.Key != ConsoleKey.Enter; cki = Console.ReadKey(true))
            {
                if (cki.Key == ConsoleKey.Backspace)
                {
                    passbits.Pop();
                }
                else
                {
                    passbits.Push(cki.KeyChar.ToString());
                }
            }
            string[] pass = passbits.ToArray();
            Array.Reverse(pass);
            return string.Join(string.Empty, pass);
        }

        private bool TryParseServiceSwitch(
            string arg, 
            string serviceSwitch, 
            out ServiceToUpdate serviceToUpdate)
        {
            serviceToUpdate = ServiceToUpdate.None;

            string serverOption;
            if (TryParseArg(arg, serviceSwitch, out serverOption)
                && !string.IsNullOrEmpty(serverOption))
            {
                if (serverOption.Equals(ServiceToUpdate.JobService.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    serviceToUpdate = ServiceToUpdate.JobService;
                    return true;
                }
                else if (serverOption.Equals(ServiceToUpdate.IntegrationService.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    serviceToUpdate = ServiceToUpdate.IntegrationService;
                    return true;
                }
            }

            return false;
        }
    }
}
