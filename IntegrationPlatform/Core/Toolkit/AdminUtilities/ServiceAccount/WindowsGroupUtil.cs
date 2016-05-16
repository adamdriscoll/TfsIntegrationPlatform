// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Security.Principal;

namespace Microsoft.TeamFoundation.Migration.Toolkit
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

        public static bool IsMemberOfLocalGroup(string localGroupName, SecurityIdentifier accountSid)
        {
            List<SecurityIdentifier> sids;
            int getMembersResult = TryGetSidsOfLocalGroupMembers(null, localGroupName, out sids);
            if (getMembersResult != Win32GroupInterop.ReturnCode.S_OK)
            {
                return false;
            }

            foreach (var sid in sids)
            {
                if (sid.Equals(accountSid))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The NetLocalGroupGetMembers function retrieves a list of the members of a particular local group in the security database, 
        /// which is the security accounts manager (SAM) database or, in the case of domain controllers, the Active Directory. Local group members can be users or global groups.
        /// </summary>
        /// <param name="serverName">The DNS or NetBIOS name of the remote server on which the function is to execute. If this parameter is null, the local computer is used.</param>
        /// <param name="localGroupName">The name of the local group whose members are to be listed. For more information, see the following Remarks section.</param>
        /// <param name="sids">The sids of the group members</param>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.TeamFoundation.Admin.Win32GroupAPI.NetApiBufferFree(System.IntPtr)")]
        public static int TryGetSidsOfLocalGroupMembers(string serverName, string localGroupName, out List<SecurityIdentifier> sids)
        {
            if (string.IsNullOrEmpty(localGroupName))
            {
                throw new ArgumentNullException("localGroupName");
            }

            sids = new List<SecurityIdentifier>();

            // Note: serverName can be (and usually is) null.
            uint entriesRead;
            uint totalEntries;
            IntPtr resumeHandle = IntPtr.Zero;
            IntPtr bufPtr = IntPtr.Zero;

            try
            {
                int returnCode = Win32GroupInterop.NetLocalGroupGetMembers(serverName,
                    localGroupName,
                    0,  // level 0. return the security identifier (SID) associated with the local group member. The bufptr parameter points to an array of LOCALGROUP_MEMBERS_INFO_0 structures
                    out bufPtr,
                    uint.MaxValue, // maximum preferred length. The method MUST allocate as much space as the data requires.
                    out entriesRead,
                    out totalEntries,
                    out resumeHandle);

                if (returnCode != Win32GroupInterop.ReturnCode.S_OK)
                {
                    return returnCode;
                    //if (returnCode == Win32GroupInterop.ReturnCode.ERROR_ACCESS_DENIED)
                    //{
                    //    //throw new UnauthorizedAccessException(AdminResources.AccessDenied());
                    //}
                    //else if (returnCode == Win32GroupInterop.ReturnCode.NERR_GroupNotFound ||
                    //         returnCode == Win32GroupInterop.ReturnCode.ERROR_NO_SUCH_ALIAS)
                    //{
                    //    //throw new ArgumentException(AdminResources.GroupNotExist(), "localGroupName");
                    //}

                    ////throw new ConfigurationException(AdminResources.ErrorOperationWithReturnCode("NetLocalGroupGetMembers", returnCode.ToString(CultureInfo.CurrentCulture)));
                }

                for (int index = 0; index < entriesRead; ++index)
                {
                    IntPtr ptr = new IntPtr((long)bufPtr + Marshal.SizeOf(typeof(Win32GroupInterop.LocalGroupMemberInfo0)) * index);

                    Win32GroupInterop.LocalGroupMemberInfo0 groupMemberInfo =
                        (Win32GroupInterop.LocalGroupMemberInfo0)Marshal.PtrToStructure(ptr, typeof(Win32GroupInterop.LocalGroupMemberInfo0));

                    SecurityIdentifier sid = new SecurityIdentifier(groupMemberInfo.Sid);

                    sids.Add(sid);
                }

                return Win32GroupInterop.ReturnCode.S_OK;
            }
            finally
            {
                if (bufPtr != IntPtr.Zero)
                {
                    int rc = Win32GroupInterop.NetApiBufferFree(bufPtr);

                    if (rc != Win32GroupInterop.ReturnCode.S_OK)
                    {
                        TraceManager.TraceError("Failed to free buffer returned by NetLocalGroupGetMembers(). Error: {0}", rc);
                    }
                }
            }
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

        // LOCALGROUP_MEMBERS_INFO_0
        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct LocalGroupMemberInfo0
        {
            public IntPtr Sid;
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

        [DllImport("NetAPI32.dll", CharSet = CharSet.Unicode)]
        internal extern static int NetLocalGroupGetMembers(
            [MarshalAs(UnmanagedType.LPWStr)] string serverName,
            [MarshalAs(UnmanagedType.LPWStr)] string localGroupName,
            uint level,
            out IntPtr bufptr,
            uint preferredMaximumLength,
            out uint entriesRead,
            out uint totalEntries,
            out IntPtr resumeHandle);

        [DllImport("Netapi32.dll")]
        internal extern static int NetApiBufferFree(IntPtr Buffer);
    }
}
