// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.AccessControl;
using System.IO;

namespace InstallationCA
{
    /// <summary>
    /// A utility class that manages a local Windows User Group
    /// </summary>
    public static class WindowsGroupUtil
    {
        /// <summary>
        /// Creates a group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="groupComment"></param>
        public static void CreateGroup(string groupName, string groupComment)
        {
            Win32GroupInterop.LocalGroupInfo groupInfo = new Win32GroupInterop.LocalGroupInfo();
            groupInfo.Name = groupName;
            groupInfo.Comment = groupComment;

            int returnCode = Win32GroupInterop.NetLocalGroupAdd(null, 1, ref groupInfo, 0);

            switch (returnCode)
            {
                case Win32GroupInterop.ReturnCode.S_OK:
                case Win32GroupInterop.ReturnCode.ERROR_ALIAS_EXISTS:
                case Win32GroupInterop.ReturnCode.NERR_GroupExists:
                    break;

                default:
                    throw new Exception(string.Format("CreateGroup failed: {0}", returnCode));
            }
        }

        /// <summary>
        /// Deletes a group
        /// </summary>
        /// <param name="groupName"></param>
        public static void DeleteGroup(string groupName)
        {
            int returnCode = Win32GroupInterop.NetLocalGroupDel(null, groupName);

            switch (returnCode)
            {
                case Win32GroupInterop.ReturnCode.S_OK:
                case Win32GroupInterop.ReturnCode.ERROR_NO_SUCH_ALIAS:
                case Win32GroupInterop.ReturnCode.NERR_GroupNotFound:
                    break;

                default:
                    throw new Exception(string.Format("DeleteGroup failed: {0}", returnCode));
            }
        }

        /// <summary>
        /// Adds a user to the named group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="userName"></param>
        public static void AddMemberToGroup(string groupName, string userName)
        {
            Win32GroupInterop.LocalGroupMemberInfo memberInfo = new Win32GroupInterop.LocalGroupMemberInfo();
            memberInfo.FullName = userName;

            int returnCode = Win32GroupInterop.NetLocalGroupAddMembers(null, groupName, 3, ref memberInfo, 1);

            switch (returnCode)
            {
                case Win32GroupInterop.ReturnCode.S_OK:
                case Win32GroupInterop.ReturnCode.ERROR_MEMBER_IN_ALIAS:
                    break;

                default:
                    throw new Exception(string.Format("AddMemberToGroup failed: {0}", returnCode));
            }
        }

        /// <summary>
        /// Removes a user from the named group
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="userName"></param>
        public static void RemoveMemberFromGroup(string groupName, string userName)
        {
            Win32GroupInterop.LocalGroupMemberInfo memberInfo = new Win32GroupInterop.LocalGroupMemberInfo();
            memberInfo.FullName = userName;

            int returnCode = Win32GroupInterop.NetLocalGroupDelMembers(null, groupName, 3, ref memberInfo, 1);

            switch (returnCode)
            {
                case Win32GroupInterop.ReturnCode.S_OK:
                case Win32GroupInterop.ReturnCode.ERROR_MEMBER_NOT_IN_ALIAS:
                    break;

                default:
                    throw new Exception(string.Format("RemoveMemberFromGroup failed: {0}", returnCode));
            }
        }

        /// <summary>
        /// Set permissions for a group on a directory.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="path"></param>
        /// <param name="rights"></param>
        public static void SetGroupAcl(string groupName, string path, FileSystemRights rights)
        {
            bool result;

            // Remove trailing backslash
            path = path.TrimEnd('\\');
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            DirectorySecurity directorySecurity = directoryInfo.GetAccessControl(AccessControlSections.Access);
            
            FileSystemAccessRule directoryAccessRule = new FileSystemAccessRule(groupName, rights, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
            directorySecurity.ModifyAccessRule(AccessControlModification.Set, directoryAccessRule, out result);

            if (result == false)
            {
                throw new Exception(string.Format("SetAcl failed to set directory access on {0} for group {1}", groupName, path));
            }

            FileSystemAccessRule inheritanceAccessRule = new FileSystemAccessRule(groupName, rights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AccessControlType.Allow);
            directorySecurity.ModifyAccessRule(AccessControlModification.Add, inheritanceAccessRule, out result);

            if (result == false)
            {
                throw new Exception(string.Format("SetAcl failed to set inheritance access on {0} for group {1}", groupName, path));
            }

            directoryInfo.SetAccessControl(directorySecurity);
        }
    }

    internal static class Win32GroupInterop
    {
        internal struct ReturnCode
        {
            internal const int S_OK = 0;
            internal const int ERROR_ACCESS_DENIED = 5;
            internal const int ERROR_INVALID_PARAMETER = 87;
            internal const int ERROR_MEMBER_NOT_IN_ALIAS = 1377; // member not in a group            
            internal const int ERROR_MEMBER_IN_ALIAS = 1378; // member already exists
            internal const int ERROR_ALIAS_EXISTS = 1379;  // group already exists
            internal const int ERROR_NO_SUCH_ALIAS = 1376;
            internal const int ERROR_NO_SUCH_MEMBER = 1387;
            internal const int NERR_GroupNotFound = 2220;
            internal const int NERR_GroupExists = 2223;
            internal const int NERR_UserInGroup = 2236;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LocalGroupInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string Name;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string Comment;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LocalGroupMemberInfo
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string FullName; // domainandname
        }

        [DllImport("Netapi32.dll")]
        internal extern static int NetLocalGroupAdd([MarshalAs(UnmanagedType.LPWStr)] string servername,
                                                         int level,
                                                         ref LocalGroupInfo buf,
                                                         int parm_err);

        [DllImport("Netapi32.dll")]
        internal extern static int NetLocalGroupAddMembers([MarshalAs(UnmanagedType.LPWStr)] string serverName,
                                                         [MarshalAs(UnmanagedType.LPWStr)] string groupName,
                                                         int level,
                                                         ref LocalGroupMemberInfo buf,
                                                         int totalEntries);

        [DllImport("Netapi32.dll")]
        internal extern static int NetLocalGroupDelMembers([MarshalAs(UnmanagedType.LPWStr)] string serverName,
                                                         [MarshalAs(UnmanagedType.LPWStr)] string groupName,
                                                         int level,
                                                         ref LocalGroupMemberInfo buf,
                                                         int totalEntries);

        [DllImport("Netapi32.dll")]
        internal extern static int NetLocalGroupDel([MarshalAs(UnmanagedType.LPWStr)] string servername,
                                                        [MarshalAs(UnmanagedType.LPWStr)] string groupname);
    }
}
