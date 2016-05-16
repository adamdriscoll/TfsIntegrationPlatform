// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;
using Microsoft.TeamFoundation.Migration.EntityModel;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.Toolkit.Services;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WIT
{
    public class WITTranslationService : TranslationServiceBase
    {
        Dictionary<SourceSideTypeEnum, IdentityLookupContext> m_contexts = new Dictionary<SourceSideTypeEnum, IdentityLookupContext>();

        internal WITTranslationService(
            Session session,
            UserIdentityLookupService userIdLookupService)
            : base(session, userIdLookupService)
        {
            UserIdLookupService = userIdLookupService;

            Guid leftMigrationSourceId = new Guid(m_session.LeftMigrationSourceUniqueId);
            Guid rightMigrationSourceId = new Guid(m_session.RightMigrationSourceUniqueId);

            m_contexts.Add(SourceSideTypeEnum.Left, new IdentityLookupContext(leftMigrationSourceId, rightMigrationSourceId));
            m_contexts.Add(SourceSideTypeEnum.Right, new IdentityLookupContext(rightMigrationSourceId, leftMigrationSourceId));
        }

        #region ITranslationService Members

        public override void Translate(IMigrationAction action, Guid migrationSourceIdOfChangeGroup)
        {
            if (action.Action.Equals(WellKnownChangeActionId.SyncContext))
            {
                return;
            }

            if (IsSyncGeneratedAction(action, migrationSourceIdOfChangeGroup))
            {
                action.State = ActionState.Skipped;
                return;
            }

            MapWorkItem(action, migrationSourceIdOfChangeGroup);

            if (action.Action == WellKnownChangeActionId.Add
                || action.Action == WellKnownChangeActionId.Edit)
            {
                MapWorkItemTypeFieldValues(action, migrationSourceIdOfChangeGroup);
            }
        }      

        public override bool IsSyncGeneratedAction(IMigrationAction action, Guid migrationSourceIdOfChangeGroup)
        {
            XmlElement rootNode = action.MigrationActionDescription.DocumentElement;

            if (null == rootNode)
            {
                throw new MigrationException(MigrationToolkitResources.InvalideChangeActionDescription, action.ActionId);
            }
            string sourceItemId = rootNode.Attributes["WorkItemID"].Value;
            string sourceItemRevision = rootNode.Attributes["Revision"].Value;

            return IsSyncGeneratedItemVersion(sourceItemId, sourceItemRevision, migrationSourceIdOfChangeGroup);
        }

        public override string TryGetTargetItemId(string sourceWorkItemId, Guid sourceId)
        {
            // search in the cache first
            string targetItemId;
            if (m_migrationItemCache.TryFindMirroredItemId(sourceWorkItemId, sourceId, out targetItemId))
            {
                return targetItemId;
            }

            string targetWorkItemId = string.Empty;
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var migrationItemResult =
                    from mi in context.RTMigrationItemSet
                    where mi.ItemId.Equals(sourceWorkItemId)
                        && !mi.ItemVersion.Equals(Constants.ChangeGroupGenericVersionNumber) // exclude non-versioned migration items (e.g. VC change group)
                    select mi;
                if (migrationItemResult.Count() == 0)
                {
                    return targetWorkItemId;
                }

                RTMigrationItem sourceItem = null;
                foreach (RTMigrationItem rtMigrationItem in migrationItemResult)
                {
                    rtMigrationItem.MigrationSourceReference.Load();
                    if (rtMigrationItem.MigrationSource.UniqueId.Equals(sourceId))
                    {
                        sourceItem = rtMigrationItem;
                    }
                }
                if (null == sourceItem)
                {
                    return targetWorkItemId;
                }

                var sessionUniqueId = new Guid(m_session.SessionUniqueId);
                var itemConvPairResult =
                    from p in context.RTItemRevisionPairSet
                    where (p.LeftMigrationItem.Id == sourceItem.Id || p.RightMigrationItem.Id == sourceItem.Id)
                        && (p.ConversionHistory.SessionRun.Config.SessionUniqueId.Equals(sessionUniqueId))
                    select p;

                if (itemConvPairResult.Count() == 0)
                {
                    return targetWorkItemId;
                }

                RTItemRevisionPair itemRevisionPair = itemConvPairResult.First();
                if (itemRevisionPair.LeftMigrationItem == sourceItem)
                {
                    itemRevisionPair.RightMigrationItemReference.Load();
                    targetWorkItemId = itemRevisionPair.RightMigrationItem.ItemId;
                }
                else
                {
                    itemRevisionPair.LeftMigrationItemReference.Load();
                    targetWorkItemId = itemRevisionPair.LeftMigrationItem.ItemId;
                }

                // cache the result
                m_migrationItemCache.AddItemPair(sourceId, sourceWorkItemId, targetWorkItemId);
            }

            return targetWorkItemId;
        }

        public override void CacheItemVersion(string sourceItemId, string sourceVersionId, Guid sourceId)
        {
            base.CacheItemVersion(sourceItemId, sourceVersionId, sourceId);
            m_migrationItemCache.AddNewItemVersions(sourceId, sourceItemId, new string[] { sourceVersionId });
        }

        public override string GetLastProcessedItemVersion(string sourceItemId, Guid sourceId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var lastVersionQuery =
                    from l in context.RTLastProcessedItemVersionsSet
                    where l.ItemId.Equals(sourceItemId)
                       && l.MigrationSourceId.Equals(sourceId)
                    select l.Version;

                if (lastVersionQuery.Count() > 0)
                {
                    return lastVersionQuery.First();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public override void UpdateLastProcessedItemVersion(Dictionary<string, string> itemVersionPair, long lastChangeGroupId, Guid sourceId)
        {
            if (itemVersionPair.Count() == 0)
            {
                return;
            }

            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                foreach (var itemVerion in itemVersionPair)
                {
                    string itemId = itemVerion.Key;
                    string version = itemVerion.Value;
                    var query =
                        from l in context.RTLastProcessedItemVersionsSet
                        where l.ItemId.Equals(itemId)
                           && l.MigrationSourceId.Equals(sourceId)
                        select l;

                    if (query.Count() > 0)
                    {
                        if (string.IsNullOrEmpty(query.First().Version))
                        {
                            query.First().Version = version;
                        }
                        else
                        {
                            int inDbVersion = int.Parse(query.First().Version);
                            int newVersion = int.Parse(version);
                            if (newVersion > inDbVersion)
                            {
                                query.First().Version = version;
                            }
                        }
                    }
                    else
                    {
                        var newEntry = RTLastProcessedItemVersions.CreateRTLastProcessedItemVersions(sourceId, itemId, version);
                        context.AddToRTLastProcessedItemVersionsSet(newEntry);
                    }
                }

                context.TrySaveChanges();
            }
        }

        #endregion
        
        internal void MapWorkItemTypeFieldValues(IMigrationAction action, Guid sourceId)
        {
            MapWorkItemTypeFieldValues(action.ActionId.ToString(), action.MigrationActionDescription, sourceId);
        }

        public void MapWorkItemTypeFieldValues(string itemId, XmlDocument workItemDescriptionDocument, Guid sourceId)
        {
            XmlDocument copy = new XmlDocument();
            copy.LoadXml(workItemDescriptionDocument.OuterXml);

            XmlElement rootNode = workItemDescriptionDocument.DocumentElement;
            if (null == rootNode)
            {
                throw new MigrationException(MigrationToolkitResources.InvalideChangeActionDescription, itemId);
            }

            bool isLeft = DecideSidenessInConfig(sourceId, m_session);
            SourceSideTypeEnum sourceSide = isLeft ? SourceSideTypeEnum.Left : SourceSideTypeEnum.Right;

            MapOwnerUserId(rootNode, sourceSide);

            #region Map work item type

		    string sourceWorkItemType = rootNode.Attributes["WorkItemType"].Value;
            WorkItemTypeMappingElement typeMapEntry =
                m_session.WITCustomSetting.WorkItemTypes.GetMappingEntry(sourceSide, sourceWorkItemType);

            if (null == typeMapEntry)
            {
                throw new UnmappedWorkItemTypeException(sourceWorkItemType);
            }

            string targetWorkItemType = m_session.WITCustomSetting.WorkItemTypes.GetMappedType(
                sourceSide,
                sourceWorkItemType,
                typeMapEntry);

            rootNode.SetAttribute("WorkItemType", targetWorkItemType); 

	        #endregion

            if (null == typeMapEntry /* backward compatibility */
                || string.IsNullOrEmpty(typeMapEntry.fieldMap) /* backward compatibility */
                || typeMapEntry.fieldMap.Equals(WitMappingConfigVocab.All, StringComparison.OrdinalIgnoreCase))
            {
                XmlNode columnsNode = rootNode.SelectSingleNode("/WorkItemChanges/Columns");
                if (null == columnsNode)
                {
                    throw new MigrationException(MigrationToolkitResources.InvalideChangeActionDescription, itemId);
                }
                XmlNodeList columns = rootNode.SelectNodes("/WorkItemChanges/Columns/Column");
                if (null == columns)
                {
                    throw new MigrationException(MigrationToolkitResources.InvalideChangeActionDescription, itemId);
                }

                List<XmlNode> fieldsToExclude = new List<XmlNode>();
                foreach (XmlNode fieldColumn in columnsNode)
                {
                    if (IsSkippingField(fieldColumn))
                    {
                        fieldsToExclude.Add(fieldColumn);
                    }
                }

                foreach (XmlNode excludedField in fieldsToExclude)
                {
                    excludedField.ParentNode.RemoveChild(excludedField);
                }
                fieldsToExclude.Clear();
            }
            else
            {
                #region look up the field map entry to use

                FieldMap fieldMapEntry = null;
                foreach (FieldMap fieldMap in m_session.WITCustomSetting.FieldMaps.FieldMap)
                {
                    if (fieldMap.name == typeMapEntry.fieldMap)
                    {
                        fieldMapEntry = fieldMap;
                        break;
                    }
                }

                #endregion

                #region Map field and value

                if (null != fieldMapEntry)
                {
                    XmlNode columnsNode = rootNode.SelectSingleNode("/WorkItemChanges/Columns");
                    if (null == columnsNode)
                    {
                        throw new MigrationException(MigrationToolkitResources.InvalideChangeActionDescription, itemId);
                    }

                    #region map aggregated fields
                    List<string> aggregatedFields = new List<string>(); // store the aggregated fields so that we won't delete them later
                    if (fieldMapEntry.AggregatedFields.FieldsAggregationGroup.Count() > 0)
                    {
                        foreach (var fieldsGroup in fieldMapEntry.AggregatedFields.FieldsAggregationGroup)
                        {
                            if (fieldsGroup.MapFromSide != sourceSide || fieldsGroup.SourceField.Count == 0)
                            {
                                continue;
                            }

                            bool canUseMappingConfig = true;

                            // apply value map to the aggregated fields and identify the field columns
                            // Note:
                            // 1. Aggregated fields may appear in normal field maps
                            // 2. If an aggregated field does not appear in normal field map,
                            //    the field will be dropped after the translation
                            // 3. If a field appear in both aggregated and normal field mapping,
                            //    it may use different value maps
                            foreach (SourceField srcField in fieldsGroup.SourceField)
                            {
                                XmlNode srcFieldCol = rootNode.SelectSingleNode(
                                    string.Format("/WorkItemChanges/Columns/Column[@ReferenceName='{0}']", srcField.SourceFieldName));

                                if (null != srcFieldCol)
                                {
                                    // map field value and cache the result
                                    srcField.FieldColumnNode = srcFieldCol;

                                    string mappedValue;
                                    if (GetMappedFieldValue(sourceSide, srcFieldCol.FirstChild.InnerText, srcField.valueMap, copy.DocumentElement, out mappedValue))
                                    {
                                        srcField.MappedValue = mappedValue;
                                    }
                                    else
                                    {
                                        srcField.MappedValue = srcFieldCol.FirstChild.InnerText;
                                    }
                                }
                                else
                                {
                                    // one of the aggregated field is not present in the WIT description document
                                    TraceManager.TraceInformation(
                                        "Aggregating field '{0}' is not in the WIT update document of Change Action {1}'",
                                        srcField.SourceFieldName,
                                        itemId);

                                    canUseMappingConfig = false;
                                    break;
                                }
                            }

                            if (!canUseMappingConfig)
                            {
                                continue;
                            }

                            string[] srcFieldValues = new string[fieldsGroup.SourceField.Count];
                            foreach (SourceField srcField in fieldsGroup.SourceField)
                            {
                                if (srcField.Index >= 0 && srcField.Index < srcFieldValues.Length)
                                {
                                    srcFieldValues[srcField.Index] = srcField.MappedValue ?? string.Empty;
                                }
                                else
                                {
                                    TraceManager.TraceError(
                                        "Source aggregation field '{0}' has a wrong index of '{1}' in the configuration.",
                                        srcField.SourceFieldName ?? string.Empty, srcField.Index.ToString());
                                }
                            }

                            for (int i = 0; i < srcFieldValues.Length; ++i)
                            {
                                if (srcFieldValues[i] == null)
                                {
                                    TraceManager.TraceError(
                                        "Aggregated field format '{0}' has NULL value at index '{1}'.",
                                        fieldsGroup.Format, i.ToString());
                                    canUseMappingConfig = false;
                                }
                            }

                            if (!canUseMappingConfig)
                            {
                                continue;
                            }

                            try
                            {
                                string aggregatedFieldName = fieldsGroup.TargetFieldName;
                                string aggregatedFieldValue = string.Format(
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    fieldsGroup.Format, srcFieldValues);

                                // first, try looking for the field in the document (the field may exist on source side
                                // and included in the delta document)
                                XmlNode aggregatedFieldInDoc = rootNode.SelectSingleNode(
                                    string.Format("/WorkItemChanges/Columns/Column[@ReferenceName='{0}']", aggregatedFieldName));

                                if (null == aggregatedFieldInDoc)
                                {
                                    XmlElement aggregatedFieldCol = columnsNode.OwnerDocument.CreateElement("Column");
                                    aggregatedFieldCol.SetAttribute("ReferenceName", aggregatedFieldName);
                                    aggregatedFieldCol.SetAttribute("Type", string.Empty);
                                    aggregatedFieldCol.SetAttribute("DisplayName", string.Empty);
                                    columnsNode.AppendChild(aggregatedFieldCol);
                                    XmlElement v = columnsNode.OwnerDocument.CreateElement("Value");
                                    v.InnerText = aggregatedFieldValue;
                                    aggregatedFieldCol.AppendChild(v);
                                }
                                else
                                {
                                    aggregatedFieldInDoc.FirstChild.InnerText = aggregatedFieldValue;
                                }

                                aggregatedFields.Add(aggregatedFieldName);
                            }
                            catch (System.FormatException formatException)
                            {
                                TraceManager.TraceException(formatException);
                            }
                        }
                    }
                    #endregion

                    // map field and values
                    XmlNodeList columns = rootNode.SelectNodes("/WorkItemChanges/Columns/Column");
                    if (null == columns)
                    {
                        throw new MigrationException(MigrationToolkitResources.InvalideChangeActionDescription, itemId);
                    }

                    List<XmlNode> fieldsToExclude = new List<XmlNode>();
                    foreach (XmlNode fieldColumn in columns)
                    {
                        string srcFieldRefName = fieldColumn.Attributes["ReferenceName"].Value;
                        string srcFieldValue = fieldColumn.FirstChild.InnerText;

                        MappedField mappedFieldEntry = fieldMapEntry.MappedFields.GetMappedFieldEntry(
                            sourceSide, srcFieldRefName);
                        if (null == mappedFieldEntry)
                        {
                            if (!aggregatedFields.Contains(srcFieldRefName, StringComparer.OrdinalIgnoreCase))
                            {
                                // record the unmapped fields and delete them after the field/value mapping is applied
                                // aggregated fields are added by us and are target-specific - they shouldn't be deleted
                                fieldsToExclude.Add(fieldColumn);
                            }
                            continue;
                        }
                        else
                        {
                            string mapFromField = (isLeft ? mappedFieldEntry.LeftName : mappedFieldEntry.RightName);
                            if (mapFromField.Equals(WitMappingConfigVocab.Any, StringComparison.OrdinalIgnoreCase))
                            {
                                if (IsSkippingField(fieldColumn))
                                {
                                    fieldsToExclude.Add(fieldColumn);
                                }
                            }
                        }

                        string tgtFieldRefName = fieldMapEntry.MappedFields.GetMappedFieldName(sourceSide, srcFieldRefName, mappedFieldEntry);
                        if (string.IsNullOrEmpty(tgtFieldRefName))
                        {
                            // record the fields that are mapped to "", semantically the field is excluded on target side
                            fieldsToExclude.Add(fieldColumn);
                            continue;
                        }

                        MapFieldValue(fieldMapEntry, sourceSide, fieldColumn, srcFieldRefName, tgtFieldRefName, srcFieldValue, mappedFieldEntry.valueMap, copy.DocumentElement);
                        fieldColumn.Attributes["ReferenceName"].Value = tgtFieldRefName;
                    }

                    // delete the unmapped fields
                    foreach (XmlNode unmappedField in fieldsToExclude)
                    {
                        unmappedField.ParentNode.RemoveChild(unmappedField);
                    }
                    fieldsToExclude.Clear();
                    aggregatedFields.Clear();

                    // apply missing field & default values
                    var missingFieldMappings = fieldMapEntry.MappedFields.GetMissingFieldEntries(sourceSide);
                    foreach (MappedField mappedFieldEntry in missingFieldMappings)
                    {
                        if (null == mappedFieldEntry)
                        {
                            continue;
                        }

                        string missingFieldName = (sourceSide == SourceSideTypeEnum.Left ? mappedFieldEntry.RightName : mappedFieldEntry.LeftName);
                        XmlElement missingField = columnsNode.OwnerDocument.CreateElement("Column");
                        missingField.SetAttribute("ReferenceName", missingFieldName);
                        missingField.SetAttribute("Type", string.Empty);
                        missingField.SetAttribute("DisplayName", string.Empty);
                        columnsNode.AppendChild(missingField);

                        // note missing fields do not have "from" value, hence User Id lookup is not needed.
                        string missingFieldValue = string.Empty;
                        if (!string.IsNullOrEmpty(mappedFieldEntry.valueMap))
                        {
                            string mappedValue;
                            if (GetMappedFieldValue(sourceSide, missingFieldValue, mappedFieldEntry.valueMap, copy.DocumentElement, out mappedValue))
                            {
                                missingFieldValue = mappedValue;
                            }
                            else
                            {
                                // use the source value when there is no value map, i.e.
                                // missingFieldValue = missingFieldValue;
                            }
                        }

                        XmlElement v = columnsNode.OwnerDocument.CreateElement("Value");
                        v.InnerText = missingFieldValue;
                        missingField.AppendChild(v);
                    }
                }

                #endregion
            }
        }

        private static bool IsSkippingField(XmlNode fieldColumn)
        {
            bool skipField;
            try
            {
                XmlAttribute skippingFieldAttr = fieldColumn.Attributes["IsSkippingField"];
                if (null == skippingFieldAttr)
                {
                    skipField = false;
                }
                else
                {
                    if (!bool.TryParse(skippingFieldAttr.Value, out skipField))
                    {
                        skipField = false;
                    }
                }
            }
            catch (Exception)
            {
                skipField = false;
            }
            return skipField;
        }

        private void MapFieldValue(
            FieldMap fieldMap,
            SourceSideTypeEnum fromSide, 
            XmlNode fieldColumn,
            string srcFieldRefName,
            string tgtFieldRefName, 
            string srcFieldValue, 
            string valueMapName,
            XmlElement descriptionDocRootNode)
        {
            string mappedValue;
            if (!string.IsNullOrEmpty(valueMapName) && GetMappedFieldValue(fromSide, srcFieldValue, valueMapName, descriptionDocRootNode, out mappedValue))
            {
                // do nothing, we have the mappedValue set
            }
            else if (UserIdLookupService.IsConfigured && fieldMap.IsUserIdField(fromSide, srcFieldRefName))
            {
                // try user id lookup
                mappedValue = MapUserIdentity(fieldMap, fromSide, srcFieldRefName, tgtFieldRefName, srcFieldValue);
            }
            else
            {
                // in other cases, just use the srcFieldValue
                mappedValue = srcFieldValue;
            }

            fieldColumn.FirstChild.InnerText = mappedValue;
        }

        private string MapUserIdentity(
            FieldMap fieldMap, 
            SourceSideTypeEnum fromSide, 
            string srcFieldRefName, 
            string tgtFieldRefName, 
            string srcFieldValue)
        {
            RichIdentity srcUserId = new RichIdentity();

            UserIdFieldElement userIdField = fieldMap.GetUserIdField(fromSide, srcFieldRefName);
            srcUserId[userIdField.UserIdPropertyName] = srcFieldValue;
                    
            RichIdentity mappedUserId;
            if (UserIdLookupService.TryLookup(srcUserId, m_contexts[fromSide], out mappedUserId))
            {
                SourceSideTypeEnum toSide = (fromSide == SourceSideTypeEnum.Left) ? SourceSideTypeEnum.Right : SourceSideTypeEnum.Left;
                UserIdFieldElement tgtUserField = fieldMap.GetUserIdField(toSide, tgtFieldRefName);
                if (null != tgtUserField)
                {
                    return mappedUserId[tgtUserField.UserIdPropertyName];
                }
                else
                {
                    return srcFieldValue;
                }
            }
            else
            {
                return srcFieldValue;
            }
        }

        private void MapOwnerUserId(
            XmlElement rootNode,
            SourceSideTypeEnum fromSide)
        {
            if (!UserIdLookupService.IsConfigured)
            {
                return;
            }

            var authorAttr = rootNode.Attributes["Author"];
            if (authorAttr == null || string.IsNullOrEmpty(authorAttr.Value))
            {
                return;
            }

            RichIdentity srcUserId = new RichIdentity();
            var authorUserIdPropertyAttr = rootNode.Attributes[Constants.WITAuthorUserIdPropertyName];
            string authorUserIdProperty;
            if (authorUserIdPropertyAttr == null || string.IsNullOrEmpty(authorUserIdPropertyAttr.Value))
            {
                authorUserIdProperty = "DisplayName";
            }
            else
            {
                authorUserIdProperty = authorUserIdPropertyAttr.Value;
            }           

            srcUserId[authorUserIdProperty] = authorAttr.Value;
                    
            RichIdentity mappedUserId;
            if (UserIdLookupService.TryLookup(srcUserId, m_contexts[fromSide], out mappedUserId))
            {
                authorAttr.Value = mappedUserId.DisplayName;
            }
        }


        /// <summary>
        /// Try getting a mapped value given a migration source unique Id and the value to be mapped.
        /// </summary>
        /// <param name="fromSide">Left or Right of the migration source in the session configuration</param>
        /// <param name="sourceFieldValue">Value before the mapping rule is applied</param>
        /// <param name="valueMapName"></param>
        /// <param name="descriptionDocRootNode">The document that contains all the field information in a particular revision</param>
        /// <param name="mappedValue">Value after the mapping rule is applied</param>
        /// <returns>True if a mapping rule is applied; FALSE otherwise</returns>
        private bool GetMappedFieldValue(
            SourceSideTypeEnum fromSide,
            string sourceFieldValue,
            string valueMapName,
            XmlElement descriptionDocRootNode,
            out string mappedValue)
        {
            TraceManager.TraceVerbose("Getting mapped field value of '{0}' in Value Map '{1}'",
                sourceFieldValue ?? string.Empty, valueMapName ?? string.Empty);
            ValueMap valueMapEntry = null;
            foreach (ValueMap valueMap in m_session.WITCustomSetting.ValueMaps.ValueMap)
            {
                if (valueMap.name == valueMapName)
                {
                    valueMapEntry = valueMap;
                    break;
                }
            }

            if (null != valueMapEntry)
            {
                TraceManager.TraceVerbose("Applying mapped field value of '{0}' in Value Map '{1}'",
                    sourceFieldValue ?? string.Empty, valueMapName ?? string.Empty);
                return valueMapEntry.TryGetMappedValue(fromSide, sourceFieldValue, descriptionDocRootNode, out mappedValue);
            }
            else
            {
                TraceManager.TraceVerbose("Cannot find Value Map '{0}'", valueMapName ?? string.Empty);
                mappedValue = sourceFieldValue;
                return false;
            }
        }


        private void MapWorkItem(IMigrationAction action, Guid sourceId)
        {
            MapWorkItem(action.ActionId.ToString(), action.MigrationActionDescription, sourceId);
        }

        internal void MapWorkItem(string itemId, XmlDocument workItemDescriptionDocument, Guid sourceId)
        {
            if (null == workItemDescriptionDocument.DocumentElement)
            {
                throw new MigrationException(MigrationToolkitResources.InvalideChangeActionDescription, itemId);
            }
            string sourceWorkItemId = workItemDescriptionDocument.DocumentElement.Attributes["WorkItemID"].Value;
            string targetWorkItemId = TryGetTargetItemId(sourceWorkItemId, sourceId);
            if (!string.IsNullOrEmpty(targetWorkItemId))
            {
                workItemDescriptionDocument.DocumentElement.SetAttribute("TargetWorkItemID", targetWorkItemId);
            }
        }

        private static bool DecideSidenessInConfig(Guid sourceId, Session sessionConfig)
        {
            bool isLeft = false;
            if (new Guid(sessionConfig.LeftMigrationSourceUniqueId) == sourceId)
            {
                isLeft = true;
            }
            else
            {
                Debug.Assert(new Guid(sessionConfig.RightMigrationSourceUniqueId) == sourceId);
            }
            return isLeft;
        }
    }
}
