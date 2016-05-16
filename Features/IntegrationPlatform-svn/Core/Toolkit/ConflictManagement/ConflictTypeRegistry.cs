// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit.ConflictManagement
{
    internal class ConflictTypeRegistry
    {
        private Dictionary<Guid, ConflictType> m_conflictTypes = new Dictionary<Guid, ConflictType>();              // <ConflictTypeRefName, ConflictType>
        private Dictionary<Guid, List<Guid>> m_sourceSpecificConflictTypes = new Dictionary<Guid, List<Guid>>();    // <SourceId, Lis<ConflictTypeRefName>>
        private Dictionary<Guid, SyncOrchestrator.ConflictsSyncOrchOptions> m_typeWithExplicitSyncOrchOption =
            new Dictionary<Guid, SyncOrchestrator.ConflictsSyncOrchOptions>();
        private Dictionary<Guid, ConflictType> m_toolkitConflictTypes = new Dictionary<Guid, ConflictType>();

        public ConflictTypeRegistry(Guid sourceId)
        {
            SourceId = sourceId;
        }

        public Guid SourceId
        {
            get;
            private set;
        }

        public Dictionary<Guid, ConflictType> RegisteredConflictTypes
        {
            get
            {
                return m_conflictTypes;
            }
        }

        public Dictionary<Guid, ConflictType> RegisteredToolkitConflictTypes
        {
            get
            {
                return m_toolkitConflictTypes;
            }
        }

        public Dictionary<Guid, SyncOrchestrator.ConflictsSyncOrchOptions> RegisteredConflictTypeWithExplictSyncOrchOption
        {
            get
            {
                return m_typeWithExplicitSyncOrchOption;
            }
        }
        
        /// <summary>
        /// Register a conflict type to the manager
        /// </summary>
        /// <param name="type"></param>
        public void RegisterConflictType(ConflictType type)
        {
            if (!m_conflictTypes.ContainsKey(type.ReferenceName))
            {
                m_conflictTypes.Add(type.ReferenceName, type);
                UpdateSourceSpecificConflictTypeRegistry(this.SourceId, type);
            }
        }

        /// <summary>
        /// Register a conflict type to the manager
        /// </summary>
        /// <param name="type"></param>
        public void RegisterConflictType(Guid migrationSourceId, ConflictType type)
        {
            if (!m_conflictTypes.ContainsKey(type.ReferenceName))
            {
                m_conflictTypes.Add(type.ReferenceName, type);
                UpdateSourceSpecificConflictTypeRegistry(migrationSourceId, type);
            }
        }

        public void RegisterConflictType(
            ConflictType type,
            SyncOrchestrator.ConflictsSyncOrchOptions syncOrchestrationOption)
        {
            RegisterConflictType(type);
            if (!m_typeWithExplicitSyncOrchOption.ContainsKey(type.ReferenceName))
            {
                m_typeWithExplicitSyncOrchOption.Add(type.ReferenceName, syncOrchestrationOption);
            }
        }

        public void RegisterConflictType(
            Guid migrationSourceId,
            ConflictType type,
            SyncOrchestrator.ConflictsSyncOrchOptions syncOrchestrationOption)
        {
            RegisterConflictType(migrationSourceId, type);
            if (!m_typeWithExplicitSyncOrchOption.ContainsKey(type.ReferenceName))
            {
                m_typeWithExplicitSyncOrchOption.Add(type.ReferenceName, syncOrchestrationOption);
            }
        }

        public List<ConflictType> GetSourceSpecificConflictTypes(Guid sourceId)
        {
            var retVal = new List<ConflictType>();
            if (m_sourceSpecificConflictTypes.ContainsKey(sourceId))
            {
                foreach (var conflictTypeRefName in m_sourceSpecificConflictTypes[sourceId])
                {
                    if (m_conflictTypes.ContainsKey(conflictTypeRefName))
                    {
                        retVal.Add(m_conflictTypes[conflictTypeRefName]);
                    }
                }
            }

            return retVal;
        }

        public void RegisterToolkitConflictType(ConflictType type)
        {
            RegisterConflictType(type);

            if (!m_toolkitConflictTypes.ContainsKey(type.ReferenceName))
            {
                m_toolkitConflictTypes.Add(type.ReferenceName, type);
            }
        }

        public void RegisterToolkitConflictType(
            ConflictType type,
            SyncOrchestrator.ConflictsSyncOrchOptions syncOrchestrationOption)
        {
            RegisterConflictType(type, syncOrchestrationOption);

            if (!m_toolkitConflictTypes.ContainsKey(type.ReferenceName))
            {
                m_toolkitConflictTypes.Add(type.ReferenceName, type);
            }
        }

        public void ValidateAndSaveProviderConflictRegistration(int providerInternalId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                RTProvider providerCache = (from p in context.RTProviderSet
                                            where p.Id == providerInternalId
                                            select p).First();
                Debug.Assert(null != providerCache, "null == providerCache");
                Guid providerReferenceName = providerCache.ReferenceName;

                List<ConflictType> unsavedTypes = new List<ConflictType>(this.RegisteredConflictTypes.Count);
                bool validateToolkitConflictTypes = providerReferenceName.Equals(Constants.FrameworkSourceId);
                if (validateToolkitConflictTypes)
                {
                    foreach (var c in this.RegisteredToolkitConflictTypes)
                    {
                        unsavedTypes.Add(c.Value);
                    }
                }
                else
                {
                    foreach (var c in this.RegisteredConflictTypes)
                    {
                        if (!this.RegisteredToolkitConflictTypes.ContainsKey(c.Key))
                        {
                            unsavedTypes.Add(c.Value);
                        }
                    }
                }
                ValidateSaveConflictType(context, providerReferenceName, providerCache, unsavedTypes, validateToolkitConflictTypes);
                ValidateSaveResolutionAction(context, providerReferenceName, providerCache, unsavedTypes, validateToolkitConflictTypes);

                context.TrySaveChanges();
            }
        }

        private void ValidateSaveResolutionAction(
            RuntimeEntityModel context,
            Guid providerReferenceName,
            RTProvider providerCache,
            List<ConflictType> typesToValidate,
            bool validateToolkitConflictTypes)
        {
            // mark all currently active types to be inactive
            var currActionsQuery =
                from ra in context.RTResolutionActionSet
                where ra.Provider.ReferenceName.Equals(providerReferenceName)
                   && ra.IsActive.Value
                select ra;

            // toolkit resolution actions are incremental
            if (!validateToolkitConflictTypes)
            {
                foreach (var resolutionAction in currActionsQuery)
                {
                    resolutionAction.IsActive = false;
                }
            }

            // mark actions that are supported by new version to be active
            // and also add those that are new
            List<ResolutionAction> unsavedActions = new List<ResolutionAction>(this.RegisteredConflictTypes.Count);
            foreach (var c in typesToValidate)
            {
                foreach (var resolutionAction in c.SupportedResolutionActions)
                {
                    var searchInExistingActions =
                        from ra in currActionsQuery
                        where ra.ReferenceName.Equals(resolutionAction.Key)
                        select ra;

                    if (searchInExistingActions.Count() > 0)
                    {
                        searchInExistingActions.First().IsActive = true;
                    }
                    else
                    {
                        CreateNewResolutionAction(context, resolutionAction.Value, providerCache);
                    }
                }
            }
        }

        private void CreateNewResolutionAction(RuntimeEntityModel context, ResolutionAction unsavedAction, RTProvider providerCache)
        {
            RTResolutionAction resolutionAction = RTResolutionAction.CreateRTResolutionAction(0, unsavedAction.ReferenceName, unsavedAction.FriendlyName);
            resolutionAction.IsActive = true;
            resolutionAction.Provider = providerCache;
            context.AddToRTResolutionActionSet(resolutionAction);
        }

        private void ValidateSaveConflictType(
            RuntimeEntityModel context,
            Guid providerReferenceName,
            RTProvider providerCache,
            List<ConflictType> typesToValidate,
            bool validateToolkitConflictTypes)
        {
            // mark all currently active types to be inactive
            var currConflictTypeQuery =
                from ct in context.RTConflictTypeSet
                where ct.Provider.ReferenceName.Equals(providerReferenceName)
                   && ct.IsActive.Value
                select ct;

            if (!validateToolkitConflictTypes)
            {
                // toolkit conflict types are incremental
                foreach (var conflictType in currConflictTypeQuery)
                {
                    conflictType.IsActive = false;
                }
            }

            // mark types that are supported by new version to be active
            // and also add those that are new
            // note that for backward compatibility, we will do an additional check before
            // creating a new type, i.e. if anyone is not associated with a provider yet, we claim it
            List<ConflictType> unsavedTypes = new List<ConflictType>(this.RegisteredConflictTypes.Count);
            foreach (var conflictType in typesToValidate)
            {
                var searchInExistingTypes =
                    from ct in currConflictTypeQuery
                    where ct.ReferenceName.Equals(conflictType.ReferenceName)
                    select ct;

                if (searchInExistingTypes.Count() > 0)
                {
                    searchInExistingTypes.First().IsActive = true;
                }
                else
                {
                    unsavedTypes.Add(conflictType);
                }
            }

            foreach (var unsavedType in unsavedTypes)
            {
                var ownerlessConflictTypeQuery =
                    from ct in context.RTConflictTypeSet
                    where ct.ReferenceName.Equals(unsavedType.ReferenceName)
                       && ct.Provider == null
                       && ct.IsActive == null
                    select ct;

                if (ownerlessConflictTypeQuery.Count() > 0)
                {
                    ownerlessConflictTypeQuery.First().IsActive = true;
                    ownerlessConflictTypeQuery.First().Provider = providerCache;
                }
                else
                {
                    CreateNewConflictType(context, unsavedType, providerCache);
                }
            }
        }

        private void CreateNewConflictType(
            RuntimeEntityModel context,
            ConflictType conflictType,
            RTProvider providerCache)
        {
            RTConflictType rtConflictType = RTConflictType.CreateRTConflictType(0, conflictType.ReferenceName, conflictType.FriendlyName);
            rtConflictType.IsActive = true;
            rtConflictType.Provider = providerCache;
            context.AddToRTConflictTypeSet(rtConflictType);
        }

        private void UpdateSourceSpecificConflictTypeRegistry(Guid sourceId, ConflictType type)
        {
            if (!m_sourceSpecificConflictTypes.ContainsKey(sourceId))
            {
                m_sourceSpecificConflictTypes.Add(sourceId, new List<Guid>());
            }
            if (!m_sourceSpecificConflictTypes[sourceId].Contains(type.ReferenceName))
            {
                m_sourceSpecificConflictTypes[sourceId].Add(type.ReferenceName);
            }
        }
    }
}
