// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.Services
{
    /// <summary>
    /// All well known change action Ids. 
    /// </summary>
    public static class WellKnownChangeActionId
    {
        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Unknown" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is AA81232C-4F07-478d-ADB0-797A2013C9EF.</remarks>
        public readonly static Guid Unknown = new Guid("AA81232C-4F07-478d-ADB0-797A2013C9EF");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Add" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is CB71D043-BEDE-4092-AA87-CF0F14586625.</remarks>
        public readonly static Guid Add = new Guid("CB71D043-BEDE-4092-AA87-CF0F14586625");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Edit" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is E876681D-8FF1-4342-A0A1-DB91513116B5.</remarks>
        public readonly static Guid Edit = new Guid("E876681D-8FF1-4342-A0A1-DB91513116B5");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Rename" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is 90F9D977-7F2B-4799-9014-786EC62DFC80.</remarks>
        public readonly static Guid Rename = new Guid("90F9D977-7F2B-4799-9014-786EC62DFC80");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Delete" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is 45213A63-DE99-4eab-A255-1B477C8C52C9.</remarks>
        public readonly static Guid Delete = new Guid("45213A63-DE99-4eab-A255-1B477C8C52C9");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Undelete" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is E14F3EAA-B7EB-4ec2-9182-E9660DD800B5.</remarks>
        public readonly static Guid Undelete = new Guid("E14F3EAA-B7EB-4ec2-9182-E9660DD800B5");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Branch" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is DF249D50-FA3F-466f-B2E5-247FF0592911.</remarks>
        public readonly static Guid Branch = new Guid("DF249D50-FA3F-466f-B2E5-247FF0592911");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Merge" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is 745C5F7E-926C-42b9-83E7-44F616BA0683.</remarks>
        public readonly static Guid Merge = new Guid("745C5F7E-926C-42b9-83E7-44F616BA0683");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Branch|Merge" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is B4A069CD-85D9-4bf3-9D8A-26C89E8842D3.</remarks>
        public readonly static Guid BranchMerge = new Guid("B4A069CD-85D9-4bf3-9D8A-26C89E8842D3");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Label" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is BEA89A5D-E367-4e37-A358-032FC0D3D56C.</remarks>
        public readonly static Guid Label = new Guid("BEA89A5D-E367-4e37-A358-032FC0D3D56C");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Encoding" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is 6AB4AB35-55B1-4bff-9204-116CA840EC60.</remarks>
        public readonly static Guid Encoding = new Guid("6AB4AB35-55B1-4bff-9204-116CA840EC60");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "AddAttachment" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is DBF96ACF-871E-43aa-83E4-534BCC14D71F.</remarks>
        public readonly static Guid AddAttachment = new Guid("DBF96ACF-871E-43aa-83E4-534BCC14D71F");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Delete Attachment" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is 7FEB5531-4A7D-46c6-81EF-AF2B7CB997C8.</remarks>
        public readonly static Guid DelAttachment = new Guid("7FEB5531-4A7D-46c6-81EF-AF2B7CB997C8");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Context Synchronization" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is 60F2D048-58EB-4e2a-BD00-386B63D3D63F.</remarks>
        public readonly static Guid SyncContext = new Guid("60F2D048-58EB-4e2a-BD00-386B63D3D63F");

        /// <summary>
        /// Provides the predefined ChangeActionId Guid for "Add file properties" actions.
        /// </summary>
        /// <remarks>The Guid indicated by this well known ChangeAction is 4B1B3696-E5F7-4f06-A796-3613877C18D6.</remarks>
        public readonly static Guid AddFileProperties = new Guid("4B1B3696-E5F7-4f06-A796-3613877C18D6");
    }

    /// <summary>
    /// The basic TFS action handler delegate type
    /// </summary>
    /// <param name="action">The change action being processed</param>
    /// <param name="group">The change group this change is to be a part of</param>
    public delegate void ChangeActionHandler(MigrationAction action, ChangeGroup group);

    /// <summary>
    /// Change action registration service class
    /// </summary>
    public class ChangeActionRegistrationService : IServiceProvider
    {
        readonly Dictionary<Guid, Dictionary<string, ChangeActionHandler>> m_changeActions;

        /// <summary>
        /// Change actions registered.
        /// </summary>
        public Dictionary<Guid, Dictionary<string, ChangeActionHandler>> ChangeActions
        {
            get
            {
                return m_changeActions;
            }
        }

        /// <summary>
        /// Provides a method to get the service of current object.
        /// </summary>
        /// <param name="serviceType">Type of the service being requested</param>
        /// <returns>Returns this service object if the requested type is ChangeActionRegistrationService; otherwise, null is returned.</returns>
        public object GetService(Type serviceType)
        {
            if (serviceType.Equals(typeof(ChangeActionRegistrationService)))
            {
                return this;
            }
            return null;
        }

        /// <summary>
        /// Constructor of ChangeActionRegistrationService.
        /// </summary>
        internal ChangeActionRegistrationService()
        {
            m_changeActions = new Dictionary<Guid, Dictionary<string, ChangeActionHandler>>();
        }

        /// <summary>
        /// Register an action with ChangeActionRegistrationService
        /// </summary>
        /// <param name="changeActionId">Guid representing the requested change action</param>
        /// <param name="contentTypeRefName">String containing the reference name for the content type</param>
        /// <param name="changeActionHandler">ChangeActionHandler used to handle the specified ChangeActionId and ContentType</param>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.Services.WellKnownChangeActionId"/>
        /// <seealso cref="Microsoft.TeamFoundation.Migration.Toolkit.Services.WellKnownContentType"/>
        public void RegisterChangeAction(Guid changeActionId, string contentTypeRefName, ChangeActionHandler changeActionHandler)
        {
            if (changeActionId.Equals(Guid.Empty))
            {
                throw new ArgumentException("changeActionId");
            }

            if (string.IsNullOrEmpty(contentTypeRefName))
            {
                throw new ArgumentNullException("contentTypeRefName");
            }

            if (m_changeActions.ContainsKey(changeActionId))
            {
                Debug.Assert(null != m_changeActions[changeActionId]);
                if (m_changeActions[changeActionId].ContainsKey(contentTypeRefName))
                {
                    m_changeActions[changeActionId].Remove(contentTypeRefName);
                }
            }
            else
            {
                m_changeActions.Add(changeActionId, new Dictionary<string, ChangeActionHandler>());
            }
            m_changeActions[changeActionId].Add(contentTypeRefName, changeActionHandler);
        }

        public bool TryGetChangeActionHandler(Guid changeActionId, string contentTypeRefName, out ChangeActionHandler changeActionHandler)
        {
            changeActionHandler = null;

            if (changeActionId.Equals(Guid.Empty) || string.IsNullOrEmpty(contentTypeRefName))
            {
                return false;
            }

            if (m_changeActions.ContainsKey(changeActionId) &&
                m_changeActions[changeActionId].ContainsKey(contentTypeRefName))
            {
                changeActionHandler = m_changeActions[changeActionId][contentTypeRefName];
                return true;
            }

            return false;
        }
    }
}
