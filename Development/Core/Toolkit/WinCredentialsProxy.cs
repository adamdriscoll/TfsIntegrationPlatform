// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using WinCredentials;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public static class WinCredentialsProxy
    {
        public static CredentialSet LoadCredentials(string filter)
        {
            return new CredentialSet(filter);
        }

        public static bool CredentialExists(
            string targetName,
            CredentialType credentialType)
        {
            return Credential.Exists(targetName, credentialType);
        }

        public static void AddCredential(
            string targetName,
            CredentialType credentialType,
            string userName,
            string password,
            CredentialPersistence persistence,
            string description)
        {
            Credential newCred = null;
            try
            {
                if (!Credential.Exists(targetName, credentialType))
                {
                    SecureString secPassword = new SecureString();

                    foreach (char element in password)
                    {
                        secPassword.AppendChar(element);
                    }

                    newCred = new Credential(
                        targetName, credentialType, userName, secPassword, persistence, description);

                    newCred.Save();
                }
            }
            finally
            {
                if (null != newCred)
                {
                    newCred.Dispose();
                }
            }
        }

        public static void AddGeneralCredential(
            string targetName,
            string userName,
            string password,
            CredentialPersistence persistence,
            string description)
        {
            AddCredential(targetName, CredentialType.Generic, userName, password, persistence, description);
        }

        public static void DeleteCredential(Credential credToDel)
        {
            if (null != credToDel)
            {
                credToDel.Delete();
                credToDel.Dispose();
            }
        }

        public static void DeleteCredential(string filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                var creds = LoadCredentials(filter);
                for (int i = 0; i < creds.Count; ++i)
                {
                    creds[i].Delete();
                }
            }
        }

        public static string GetPlaintextPassword(Credential credential)
        {
            IntPtr bstr = Marshal.SecureStringToBSTR(credential.Password);

            try
            {
                return Marshal.PtrToStringBSTR(bstr);
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        public static void DeleteCredential(string filter, CredentialType credType)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                using (CredentialSet creds = LoadCredentials(filter))
                {
                    for (int i = 0; i < creds.Count; ++i)
                    {
                        var cred = creds[i];
                        if (cred.Type == credType)
                        {
                            cred.Delete();
                        }
                    }
                }
            }
        }
    }
}
