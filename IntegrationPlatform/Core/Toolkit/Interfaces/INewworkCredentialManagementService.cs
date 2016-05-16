// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using WinCredentials;
using System.Collections.ObjectModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public interface INewworkCredentialManagementService
    {
        /// <summary>
        /// Add a network credential to the Windows credential store
        /// </summary>
        /// <param name="uriPrefix"></param>
        /// <param name="credType"></param>
        /// <param name="cred"></param>
        void AddCredential(
            Uri uriPrefix,
            CredentialType credType,
            NetworkCredential cred);

        /// <summary>
        /// Deletes all the credentials associated with a resource URI
        /// </summary>
        /// <param name="uriPrefix"></param>
        void DeleteCredential(
            Uri uriPrefix);

        /// <summary>
        /// Deletes a typed credential associated with a resource URI
        /// </summary>
        /// <param name="uriPrefix"></param>
        /// <param name="credType"></param>
        void DeleteCredential(
            Uri uriPrefix,
            CredentialType credType);

        /// <summary>
        /// Gets all the credentials associated with a resource URI
        /// </summary>
        /// <param name="uriPrefix"></param>
        /// <returns></returns>
        ReadOnlyCollection<NetworkCredential> GetCredentials(
            Uri uriPrefix);

        /// <summary>
        /// Gets a typed credential associated with a resource URI
        /// </summary>
        /// <param name="uriPrefix"></param>
        /// <param name="credType"></param>
        NetworkCredential GetCredential(
            Uri uriPrefix,
            CredentialType credType);

        /// <summary>
        /// Gets whether a credential is in the Windows credential stroe
        /// </summary>
        /// <param name="uriPrefix"></param>
        /// <param name="credType"></param>
        /// <returns></returns>
        bool IsCredentialStored(
            Uri uriPrefix,
            CredentialType credType);
    }
}
