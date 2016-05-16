using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Shell.Tfs11ShellAdapter
{
    internal enum CachedCredentialsType
    {
        Windows,
        ServiceIdentity,
        Other,
    }

    internal static class CredentialsCacheConstants
    {
        public static String CredentialsTypeKeyword = "Microsoft_TFS_CredentialsType";
        public static String NonInteractiveKeyword = "Microsoft_TFS_NonInteractive";
    }

    internal static class CredentialsCacheManager
    {
        public static int StoreCredentials(Uri uri, String userName, String password)
        {
            return StoreCredentials(uri, userName, password, CachedCredentialsType.Other, true);
        }

        public static int StoreCredentials(Uri uri, String userName, String password, CachedCredentialsType type, Boolean nonInteractive)
        {
            return StoreCredentials(uri, userName, CreateSecureString(password), type, nonInteractive);
        }

        public static int StoreCredentials(Uri uri, String userName, SecureString password, CachedCredentialsType type, Boolean nonInteractive)
        {
            Dictionary<String, String> attributes = new Dictionary<String, String>();
            attributes[CredentialsCacheConstants.CredentialsTypeKeyword] = type.ToString();
            attributes[CredentialsCacheConstants.NonInteractiveKeyword] = nonInteractive.ToString();

            return StoreCredentials(uri.AbsoluteUri, userName, password, null, attributes);
        }

        /// <summary>
        /// Persist the provided credentials in the Windows credentials manager for this user.
        /// The credentials are stored until the user logs off.
        /// </summary>
        /// <param name="uri">Uri to serve as the key in the credentials manager</param>
        /// <param name="username">Username to store</param>
        /// <param name="password">Password to store</param>
        /// <param name="credentialsType">The type of credentials (e.g., Windows, AcsServiceIdentity)</param>
        /// <returns>ERROR_SUCCESS (0) if successful. Otherwise, a Win32 error code returned by CredWrite.</returns>
        public static int StoreCredentials(String targetName, String userName, SecureString password, String comment, Dictionary<String, String> attributes)
        {
            NativeMethods.CREDENTIAL toPersist = new NativeMethods.CREDENTIAL();
            NativeMethods.CREDENTIAL_ATTRIBUTE[] toPersistAttributes = new NativeMethods.CREDENTIAL_ATTRIBUTE[attributes.Count];
            toPersist.CredentialBlob = IntPtr.Zero;

            try
            {
                toPersist.Flags = 0;
                toPersist.Type = (int)NativeMethods.CRED_TYPE_GENERIC;
                toPersist.TargetName = targetName;
                toPersist.Comment = comment;
                toPersist.CredentialBlob = Marshal.SecureStringToCoTaskMemUnicode(password);
                toPersist.CredentialBlobSize = password.Length * sizeof(char);
                toPersist.Persist = NativeMethods.CRED_PERSIST_LOCAL_MACHINE;

                if (attributes.Count > 0)
                {
                    int attributeIndex = 0;
                    int totalSize = 0;

                    // Convert the managed dictionary into an array of managed CREDENTIAL_ATTRIBUTE and keep track of
                    // how much memory we're using
                    foreach (KeyValuePair<String, String> attributePair in attributes)
                    {
                        NativeMethods.CREDENTIAL_ATTRIBUTE toPersistAttribute = new NativeMethods.CREDENTIAL_ATTRIBUTE();

                        toPersistAttribute.Keyword = attributePair.Key;
                        toPersistAttribute.Flags = 0;
                        toPersistAttribute.ValueSize = attributePair.Value.Length * sizeof(char);
                        toPersistAttribute.Value = Marshal.StringToCoTaskMemUni(attributePair.Value);

                        totalSize += Marshal.SizeOf(toPersistAttribute);
                        toPersistAttributes[attributeIndex++] = toPersistAttribute;
                    }

                    // Block off the space for the entire array and assign the toPersist.Attributes the pointer to the head
                    // of the list
                    IntPtr pAttribute = Marshal.AllocCoTaskMem(totalSize);
                    toPersist.AttributeCount = attributes.Count;
                    toPersist.Attributes = pAttribute;

                    // Start copying the attributes into the memory block we allocated
                    for (attributeIndex = 0; attributeIndex < toPersistAttributes.Length; attributeIndex++)
                    {
                        Marshal.StructureToPtr(toPersistAttributes[attributeIndex], pAttribute, false);
                        pAttribute = new IntPtr(pAttribute.ToInt64() + Marshal.SizeOf(toPersistAttributes[attributeIndex]));
                    }
                }
                else
                {
                    toPersist.AttributeCount = 0;
                    toPersist.Attributes = IntPtr.Zero;
                }

                toPersist.TargetAlias = null;
                toPersist.UserName = userName;

                bool retVal = NativeMethods.CredWrite(ref toPersist, 0);

                if (!retVal)
                {
                    return Marshal.GetLastWin32Error();
                }

                return NativeMethods.ERROR_SUCCESS;
            }
            finally
            {
                if (IntPtr.Zero != toPersist.CredentialBlob)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(toPersist.CredentialBlob);
                    toPersist.CredentialBlob = IntPtr.Zero;
                }

                if (IntPtr.Zero != toPersist.Attributes)
                {
                    Marshal.FreeCoTaskMem(toPersist.Attributes);
                    toPersist.Attributes = IntPtr.Zero;
                }

                for (int attributeIndex = 0; attributeIndex < toPersistAttributes.Length; attributeIndex++)
                {
                    NativeMethods.CREDENTIAL_ATTRIBUTE toPersistAttribute = toPersistAttributes[attributeIndex];

                    if (IntPtr.Zero != toPersistAttribute.Value)
                    {
                        Marshal.ZeroFreeCoTaskMemUnicode(toPersistAttribute.Value);
                        toPersistAttribute.Value = IntPtr.Zero;
                    }
                }
            }
        }

        public static Boolean DeleteCredentials(Uri uri)
        {
            return NativeMethods.CredDelete(uri.AbsoluteUri, NativeMethods.CRED_TYPE_GENERIC, 0);
        }

        private static SecureString CreateSecureString(string str)
        {
            SecureString ss = new SecureString();

            if (!string.IsNullOrEmpty(str))
            {
                foreach (char c in str)
                {
                    ss.AppendChar(c);
                }
            }

            return ss;
        }
    }

    #region CredentialsProviderHelper

    internal static class CredentialsProviderHelper
    {
        /// <summary>
        /// Given a URI and credentials which failed to authenticate, returns
        /// the username which failed to authenticate, in DOMAIN\user form.
        /// </summary>
        /// <param name="uri">Uri to which the credentials failed to authenticate</param>
        /// <param name="failedCredentials">Credentials which failed to authenticate</param>
        /// <returns>DOMAIN\user which failed to authenticate, or String.Empty</returns>
        public static String FailedUserName(Uri uri, ICredentials failedCredentials)
        {
            String userName = String.Empty;

            if (failedCredentials != null)
            {
                NetworkCredential cred = failedCredentials.GetCredential(uri, null);

                if (cred != null)
                {
                    if (cred.Domain.Length > 0 && cred.UserName.Length > 0)
                    {
                        userName = String.Concat(cred.Domain, "\\", cred.UserName);
                    }
                    else
                    {
                        userName = cred.UserName;
                    }

                    // Don't return a username that exceeds CREDUI_MAX_USERNAME_LENGTH.
                    if (userName.Length > NativeMethods.CREDUI_MAX_USERNAME_LENGTH)
                    {
                        userName = userName.Substring(0, NativeMethods.CREDUI_MAX_USERNAME_LENGTH);
                    }
                }
            }

            return userName;
        }
    }

    #endregion
}

