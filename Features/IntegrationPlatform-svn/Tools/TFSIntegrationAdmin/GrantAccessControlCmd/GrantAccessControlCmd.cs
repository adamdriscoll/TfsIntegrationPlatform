// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace TFSIntegrationAdmin.GrantAccessControlCmd
{
    internal class GrantAccessControlCmd : CommandBase
    {
        public override string CommandName
        {
            get { return "ConfigAccessControl"; /* do not localize */ }
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
            else if (cmdSpecificArgs.Length == 0)
            {
                return true;
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
                return new GrantAccessControlRslt(
                    string.Format(ResourceStrings.ErrorNeedAdminPrivilegeToRunCommandFormat, this.CommandName), this);
            }

            try
            {
                WindowsGroupUtil.CreateGroup(Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationExecWorkProcessGroupName,
                    Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationExecWorkProcessGroupComment);
            }
            catch (Exception e)
            {
                return new GrantAccessControlRslt(e.Message, this);
            }

            string dataFolderPath = string.Empty;
            try
            {
                dataFolderPath = GlobalConfiguration.WorkSpaceRoot;
            }
            catch (System.UnauthorizedAccessException)
            {
                //     The caller does not have the required permission.
                return new GrantAccessControlRslt(
                    string.Format(ResourceStrings.ErrorUnauthorizedAccessCreatingDataFolderInfoFormat, GlobalConfiguration.GlobalConfigPath), this);
            }
            catch (System.ArgumentNullException)
            {
                // path is null
                return new GrantAccessControlRslt(
                    string.Format(ResourceStrings.ErrorDataFolderInvalidPathInfoFormat, GlobalConfiguration.GlobalConfigPath), this);
            }
            catch (System.ArgumentException)
            {
                //     path is a zero-length string, contains only white space, or contains one
                //     or more invalid characters as defined by System.IO.Path.InvalidPathChars.-or-path
                //     is prefixed with, or contains only a colon character (:).
                return new GrantAccessControlRslt(
                    string.Format(ResourceStrings.ErrorDataFolderInvalidPathInfoFormat, GlobalConfiguration.GlobalConfigPath), this);
            }
            catch (System.IO.PathTooLongException)
            {
                //     The specified path, file name, or both exceed the system-defined maximum
                //     length. For example, on Windows-based platforms, paths must be less than
                //     248 characters and file names must be less than 260 characters.
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                //     The specified path is invalid (for example, it is on an unmapped drive).
                return new GrantAccessControlRslt(
                    string.Format(ResourceStrings.ErrorDataFolderInvalidPathInfoFormat, GlobalConfiguration.GlobalConfigPath), this);
            }
            catch (System.IO.IOException)
            {
                //     The directory specified by path is read-only.
                // eat this exception
            }
            catch (System.NotSupportedException)
            {
                //     path contains a colon character (:) that is not part of a drive label ("C:\").
                return new GrantAccessControlRslt(
                    string.Format(ResourceStrings.ErrorDataFolderInvalidPathInfoFormat, GlobalConfiguration.GlobalConfigPath), this);
            }

            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(dataFolderPath), "Data fold path is null or empty");
            try
            {
                WindowsGroupUtil.SetGroupAcl(Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationExecWorkProcessGroupName,
                    dataFolderPath, System.Security.AccessControl.FileSystemRights.FullControl);

                return new GrantAccessControlRslt(this);
            }
            catch (System.UnauthorizedAccessException)
            {
                //     The current process does not have access to open the file.
                return new GrantAccessControlRslt(
                    string.Format(ResourceStrings.ErrorUnauthorizedAccessUpdatingDataFolderInfoFormat, dataFolderPath), this);
            }
            catch (System.PlatformNotSupportedException)
            {
                //     The current operating system is not Microsoft Windows 2000 or later.
                return new GrantAccessControlRslt(
                    ResourceStrings.ErrorUnsupportedPlatformUpdatingDataFolderInfo, this);
            }
            catch (System.SystemException)
            {
                //     The file could not be found or modified.
                return new GrantAccessControlRslt(
                    string.Format(ResourceStrings.ErrorDataFolderNotFoundOrUnmodifiableInfoFormat, dataFolderPath), this);
            }
        }

        public override string GetHelpString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Sets access control of the TFS Integration data folder for {0}:", 
                Microsoft.TeamFoundation.Migration.Toolkit.Constants.TfsIntegrationExecWorkProcessGroupComment));
            sb.AppendFormat("  {0} {1}", Program.ProgramName, CommandName);
            return sb.ToString();
        }
    }
}
