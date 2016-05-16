// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Clear Quest Converter Main file.
// Process the query and migrates the data to Currituck
// based on the retreived information

#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Threading;
using ClearQuestOleServer;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using Microsoft.TeamFoundation.Converters.WorkItemTracking;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.Win32;
using Microsoft.TeamFoundation.Converters.Reporting;
using RepStatus = Microsoft.TeamFoundation.Converters.Reporting.ReportStatisticsStatisicsDetails.MigrationStatus;
using System.Xml.Serialization;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ
{
    public sealed class CQConverter : IWorkItemConverter
    {
        #region Private Members
        // parameters received from the init
        private ConverterParameters m_convParams;

        // handle to CQ Connection
        private CQConnection m_cqConnection;

        // field map handles for each schema
        // key is the source schema name
        private Dictionary<string, FieldMaps> m_schemaFieldMap;

        // VSTS connection based on the BISURI + Project
        private VSTSConnection m_vstsConn;

        // contains set of parameters to be passed to function
        private CQConverterParams m_cqParams;

        private static Thread m_progressThread;

        private Dictionary<string, LinkingLinkTypeMapping> m_linkTypeMaps;

        #endregion Private Members

        #region Static Members
        internal static int TotalRecords = 0;
        internal static int RecordsProcessed = 0;
        internal static Hashtable FailedRecords = new Hashtable(TFStringComparer.OrdinalIgnoreCase);
        #endregion Static Members

        /// <summary>
        /// Default Constructor
        /// </summary>
        public CQConverter()
        {
            m_cqParams = new CQConverterParams();
            m_cqParams.entityRecords = new Dictionary<string, CQEntity>(TFStringComparer.OrdinalIgnoreCase);
            m_schemaFieldMap = new Dictionary<string, FieldMaps>(TFStringComparer.OrdinalIgnoreCase);
            m_linkTypeMaps = new Dictionary<string, LinkingLinkTypeMapping>();
        }

        /// <summary>
        /// Initialize the object with the ConverterParameter values
        /// Also validates the Schema Mapping field Mapping on the CQ side
        /// </summary>
        /// <param name="convParams"></param>
        public void Initialize(ConverterParameters convParams)
        {
            // store it for future use
            this.m_convParams = convParams;

            m_cqParams.schemaMaps = convParams.SchemaMaps;
            m_cqParams.exitOnError = convParams.ExitOnError;

            // create the connection to VSTS and ensure that the user
            // is part of Service Accounts group
            m_vstsConn = new VSTSConnection(m_convParams.TargetConfig);
            m_cqParams.vstsConn = m_vstsConn;
            CQConstants.VstsConn = m_vstsConn;

            // create a ClearQuest connection handle
            m_cqConnection = new CQConnection(convParams.SourceConfig, convParams.ConfigFile);

            // check for valid CQ installation..
            m_cqParams.uniqueInstId = GetUniqueInstallationId();

            // set the output directory even before making a connection to CQ..
            // IFF some output directory is specified in CQConfig.xml (bug#12110)
            // and only if it is Analyze phase.. no file is dumped in Migration phase in o/p dir
            if (m_convParams.OutputDirectory != null &&
                m_convParams.OutputDirectory.Length >= 0)
            {
                CQConverterUtil.SetOutputDirectory(m_convParams.OutputDirectory, m_convParams.ConfigFile);
            }

            // create a user session and a admin session and execute the query
            m_cqConnection.Initialize();

            // process the schema xml file
            // validate the entities on CQ database
            // for each entity validate the field map on CQ databases
            m_cqParams.cqSession = m_cqConnection.GetUserSession();
            ConverterMain.MigrationReport.Converter = ReportConverter.CQConverter;
        } // end of Initialize

        /// <summary>
        /// Starts the actual schema migration followed by data migration
        /// </summary>
        public void Convert()
        {
            Session cqSession = m_cqConnection.GetUserSession();
            OAdQuerydef qryDef = m_cqConnection.QueryDefinition;

            // get the base entity definition to analyze
            string baseEntityDefName = CQWrapper.GetPrimaryEntityDefName(qryDef);
            Debug.Assert(baseEntityDefName != null);

            Logger.WritePerf(LogSource.CQ, "Start Analyze");
            // set o/p directory only if it is not specified in the CQConfig file..
            // otherwise its already set / created in Initialize section
            if (String.IsNullOrEmpty(m_convParams.OutputDirectory))
            {
                CQConverterUtil.SetOutputDirectory(baseEntityDefName, m_convParams.ConfigFile);
            }

            if (baseEntityDefName != null)
            {
                string[] refEntities = CQConverterUtil.GetReferencedEntityDefNames(cqSession, baseEntityDefName, m_convParams.ConfigFile);
                StringBuilder infoMsg1 = new StringBuilder(UtilityMethods.Format(CQResource.CQ_ENTITY_MIGRATED));

                foreach (string str in refEntities)
                {
                    infoMsg1.Append(str);
                    infoMsg1.Append(", "); // REVIEW - GautamG: String not localized
                }

                infoMsg1.Remove(infoMsg1.Length - 2, 1);  // remove last comma
                infoMsg1.Append(Environment.NewLine);
                Logger.Write(LogSource.CQ, TraceLevel.Info, infoMsg1.ToString());
                Display.DisplayMessage(infoMsg1.ToString());

                // create the schema map file so that for each WITD xml generation
                // the entries are added in schema map
                WITSchemaMappings schemaMap = new WITSchemaMappings();

                int reportEntityIndex = 0;
                ConverterMain.MigrationReport.Summary.SourceAndDestination.WorkItemTypes.WorkItemTypeTypes
                        = new WorkItemTypeTypes[refEntities.Length];

                foreach (string entityToMigrate in refEntities)
                {
                    if (entityToMigrate != null)
                    {
                        // process the given entity definition and generate xml for each entity
                        // one for the base entity and one for each of referenced entities
                        OAdEntityDef entityDef = CQWrapper.GetEntityDef(cqSession, entityToMigrate);
                        string entityDefName = entityDef.GetName();

                        string schemaXmlFile = entityDefName + ".xml";

                        // get the file name prepended with the path.. to be generated under base entity name folder
                        schemaXmlFile = CQConverterUtil.GetFileNameWithPath(schemaXmlFile);

                        string fieldMapXmlFile = entityDefName + CQConstants.FieldMapFileSuffix;
                        fieldMapXmlFile = CQConverterUtil.GetFileNameWithPath(fieldMapXmlFile);

                        // add the default map to schema
                        schemaMap.Mappings.AddSchemaMap(entityDefName, entityDefName,
                                schemaXmlFile, fieldMapXmlFile);

                        ConverterMain.MigrationReport.AddOutput(CQResource.Witd, schemaXmlFile);
                        ConverterMain.MigrationReport.AddOutput(CQResource.WitFieldMap, fieldMapXmlFile);

                        WITDXMLGenerator currEntityXML = new WITDXMLGenerator(schemaXmlFile, fieldMapXmlFile, cqSession, entityDef, m_vstsConn);
                        currEntityXML.GenerateSchemaXml();

                        // add the entity information in migration report
                        WorkItemTypeTypes wiType = new WorkItemTypeTypes();
                        wiType.From = wiType.To = entityDefName;
                        ConverterMain.MigrationReport.Summary.SourceAndDestination.WorkItemTypes.WorkItemTypeTypes[reportEntityIndex++] = wiType;

                        // add the link type mappings
                        MapLinkTypes(refEntities, entityToMigrate, entityDef);
                    }
                } // foreach (string entityToMigrate in refEntities)
                Display.NewLine();

                // generated the schemas and the corresponding field maps
                // finally serialize the schema map file
                string schemaMapFile = CQConverterUtil.GetFileNameWithPath(CQConstants.SchemaMapFile);
                string userMapFile = CQConverterUtil.GetFileNameWithPath(CQConstants.UserMapFile);
                ConverterMain.MigrationReport.AddOutput(CQResource.SchemaMap, schemaMapFile);
                ConverterMain.MigrationReport.AddOutput(CQResource.UserMap, userMapFile);

                schemaMap.GenerateSchemaMappings(schemaMapFile, userMapFile);
                GenerateDefaultUserMaps(userMapFile);
                ConverterMain.MigrationReport.Statistics.NumberOfItems = refEntities.Length;

                // generate the link type mapping file
                GenerateLinkTypeMappings();
            }

            Logger.WritePerf(LogSource.CQ, "End Analyze");
                    

        } // end of Convert()

        private void GenerateLinkTypeMappings()
        {
            Linking linkTypeMappingSettings = new Linking();
            LinkingLinkTypeMapping[] mappings = new LinkingLinkTypeMapping[m_linkTypeMaps.Values.Count];
            m_linkTypeMaps.Values.CopyTo(mappings, 0);
            linkTypeMappingSettings.LinkTypeMappings = mappings;

            string linkTypeMapFile = CQConverterUtil.GetFileNameWithPath(CQConstants.LinkMapFile);
            using (var fStream = new FileStream(linkTypeMapFile, FileMode.Create))
            {
                var serializer = new XmlSerializer(typeof(Linking));
                serializer.Serialize(fStream, linkTypeMappingSettings);
            }
        }

        private void MapLinkTypes(string[] refEntities, string entityToMigrate, OAdEntityDef entityDef)
        {
            List<string> refEntityList = new List<string>(refEntities);
            object[] fieldDefNameObjs = CQWrapper.GetFieldDefNames(entityDef) as object[];

            // add the field reference[list] links
            foreach (object fieldDefNameObj in fieldDefNameObjs)
            {
                string fieldDefName = fieldDefNameObj as string;
                int fieldDefType = CQWrapper.GetFieldDefType(entityDef, fieldDefName);

                if (fieldDefType == CQConstants.FIELD_REFERENCE)
                {
                    OAdEntityDef childRecordEntityDef = CQWrapper.GetFieldReferenceEntityDef(entityDef, fieldDefName);
                    string childRecordEntityDefName = CQWrapper.GetEntityDefName(childRecordEntityDef);

                    if (refEntityList.Contains(childRecordEntityDefName))
                    {
                        var linkTypeMapping = new LinkingLinkTypeMapping();
                        linkTypeMapping.LeftMigrationSourceUniqueId = "[Please add Left Migration Source Migration Id]";
                        linkTypeMapping.RightMigrationSourceUniqueId = "[Please add Right Migration Source Migration Id]";
                        linkTypeMapping.LeftLinkType = string.Format("ClearQuestAdapter.LinkType.ReferenceFieldRecordLink.{0}.{1}",
                                                                     entityToMigrate, childRecordEntityDef);
                        linkTypeMapping.RightLinkType = "[Please add Right link type reference name]";

                        if (!m_linkTypeMaps.ContainsKey(linkTypeMapping.LeftLinkType))
                        {
                            m_linkTypeMaps.Add(linkTypeMapping.LeftLinkType, linkTypeMapping);
                        }
                    }
                }
                else if (fieldDefType == CQConstants.FIELD_REFERENCE_LIST)
                {
                    OAdEntityDef childRecordEntityDef = CQWrapper.GetFieldReferenceEntityDef(entityDef, fieldDefName);
                    string childRecordEntityDefName = CQWrapper.GetEntityDefName(childRecordEntityDef);

                    if (refEntityList.Contains(childRecordEntityDefName))
                    {
                        var linkTypeMapping = new LinkingLinkTypeMapping();
                        linkTypeMapping.LeftMigrationSourceUniqueId = "[Please add Left Migration Source Migration Id]";
                        linkTypeMapping.RightMigrationSourceUniqueId = "[Please add Right Migration Source Migration Id]";
                        linkTypeMapping.LeftLinkType = string.Format("ClearQuestAdapter.LinkType.ReferenceListFieldRecordLink.{0}.{1}",
                                                                     entityToMigrate, childRecordEntityDef);
                        linkTypeMapping.RightLinkType = "[Please add Right link type reference name]";

                        if (!m_linkTypeMaps.ContainsKey(linkTypeMapping.LeftLinkType))
                        {
                            m_linkTypeMaps.Add(linkTypeMapping.LeftLinkType, linkTypeMapping);
                        }
                    }
                }
            }

            // add the duplicate links
            var duplinkTypeMapping = new LinkingLinkTypeMapping();
            duplinkTypeMapping.LeftMigrationSourceUniqueId = "[Please add Left Migration Source Migration Id]";
            duplinkTypeMapping.RightMigrationSourceUniqueId = "[Please add Right Migration Source Migration Id]";
            duplinkTypeMapping.LeftLinkType = "ClearQuestAdapter.LinkType.Duplicate";
            duplinkTypeMapping.RightLinkType = "[Please add Right link type reference name]";
            if (!m_linkTypeMaps.ContainsKey(duplinkTypeMapping.LeftLinkType))
            {
                m_linkTypeMaps.Add(duplinkTypeMapping.LeftLinkType, duplinkTypeMapping);
            }
        }

        /// <summary>
        /// Perform all the converter specific clean ups here
        /// </summary>
        public void CleanUp()
        {
            if (Directory.Exists(CQConstants.AttachmentsDir))
            {
                try
                {
                    Directory.Delete(CQConstants.AttachmentsDir, true);
                }
                catch (IOException)
                {
                    Logger.Write(LogSource.CQ, TraceLevel.Error, "Could not delete temporary attachment directory {0}",
                        CQConstants.AttachmentsDir);
                }
            }

            if (m_progressThread != null && m_progressThread.IsAlive)
            {
                m_progressThread.Abort();
                Thread.Sleep(5000); // allow the display thread to stop
            }
        }

        #region MIGRATE METHODS
        private void FindAllowedEntitiesToMigrate(string baseEntityName,
                                                  Session cqSession)
        {
            string[] refEntities = CQConverterUtil.GetReferencedEntityDefNames
                                    (cqSession, baseEntityName, m_convParams.ConfigFile);

            // check against the entity defs provided in schema map xml
            m_cqParams.allowedEntities = new Dictionary<string, bool>(TFStringComparer.OrdinalIgnoreCase);

            foreach (SchemaMapping map in m_cqParams.schemaMaps)
            {
                m_cqParams.allowedEntities.Add(map.entity, false);
            }

            bool baseEntityFound = false;
            foreach (string entity in m_cqParams.allowedEntities.Keys)
            {
                if (baseEntityName.Equals(entity, StringComparison.OrdinalIgnoreCase))
                {
                    baseEntityFound = true;
                    break;
                }
            }

            if (!baseEntityFound)
            {
                // base entity has to be migrated
                string errMsg = UtilityMethods.Format(CQResource.CQ_BASE_ENTITY_REQ,
                                                      CurConResource.Analysis,
                                                      baseEntityName, 
                                                      Path.GetFileName(m_convParams.SchemaMapFile));

                PostMigrationReport.WriteIssue(null, null, RepStatus.Failed,
                     ReportIssueType.Critical, String.Empty,
                      baseEntityName, IssueGroup.Witd, errMsg);

                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                throw new ConverterException(errMsg);
            }
            // for entries other than base entity
            // generate warning if not being selected for migration
            foreach (string entity in refEntities)
            {
                if (!m_cqParams.allowedEntities.ContainsKey(entity))
                {
                    string warningMsg = UtilityMethods.Format(CQResource.CQ_DROP_ENTITY, entity,
                                            m_convParams.SchemaMapFile);

                    Logger.Write(LogSource.CQ, TraceLevel.Warning, warningMsg);

                    PostMigrationReport.WriteIssue(null, null, RepStatus.Warning,
                         ReportIssueType.Warning, String.Empty,
                         entity, IssueGroup.Witd, warningMsg);

                    m_cqParams.allowedEntities.Remove(entity);
                }
            }
            // remove the entries from allowed entities which
            // are not referened (dir/indir) from base entity
            // foreach (string
            // set the allowed entities in allowedEntities object
        } // end of FindAllowedEntitiesToMigrate

        /// <summary>
        /// Validate all the Entity Types on Clear Quest
        /// Followed by the respective Field Mappings
        /// </summary>
        /// <returns>true if successful, false in case of some error</returns>
        private void ValidateSchemaMapOnCQ()
        {
            Session cqSession = m_cqConnection.GetUserSession();
            object[] cqEntities = (object[])CQWrapper.GetSubmitEntityDefNames(cqSession);
            Display.NewLine();

            foreach (SchemaMapping schMap in m_convParams.SchemaMaps)
            {
                string infoMsg = UtilityMethods.Format(CQResource.SchemaValidation, schMap.WITDFile);
                Logger.Write(LogSource.CQ, TraceLevel.Verbose, infoMsg);

                bool entityFoundInCQ = false;
                foreach (object obj in cqEntities)
                {
                    if (schMap.entity.Equals(obj.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        entityFoundInCQ = true;
                        break;
                    }
                }

                if (!entityFoundInCQ)
                {
                    try
                    {
                        Display.StartProgressDisplay(infoMsg);
                        string errMsg = UtilityMethods.Format(CQResource.CQ_ENTITY_NOT_EXIST,
                                                              CurConResource.Analysis, 
                                                              schMap.entity,
                                                              Path.GetFileName(m_convParams.SchemaMapFile));

                        PostMigrationReport.WriteIssue(null, null, RepStatus.Failed,
                             ReportIssueType.Critical, String.Empty,
                              schMap.entity, IssueGroup.Witd, errMsg);

                        throw new ConverterException(errMsg);
                    }
                    finally
                    {
                        Display.StopProgressDisplay();
                    }
                }
                else
                {
                    OAdEntityDef currEntityDef = CQWrapper.GetEntityDef(cqSession, schMap.entity);
                    // can validate the fields at CQ also, for this entity
                    string fieldMapFile = schMap.fieldMapFile;
                    if (fieldMapFile != null)
                    {
                        UtilityMethods.ValidateFile(fieldMapFile, schMap.schemaMapFile);
                        FieldMaps fldMaps = WITFieldMappings.CreateFromFile(fieldMapFile);
                        ValidateFieldMapOnCQ(currEntityDef, fldMaps.FieldMap, fieldMapFile);

                        // add the predefined/internal field maps
                        FieldMapsFieldMap[] internalFldMaps = GetInternalFieldMaps(fldMaps);

                        fldMaps.FieldMap.CopyTo(internalFldMaps, 0);
                        fldMaps.FieldMap = internalFldMaps;

                        // add the loaded field map in Schema Field Map for future use
                        m_schemaFieldMap.Add(schMap.entity, fldMaps);
                    }
                }
            } // end of foreach SchemaMappings
        } // end of ValidateSchemaMapOnCQ

        /// <summary>
        /// Validates the Field Map on CQ for a Entity Type
        /// </summary>
        /// <param name="entityDef">Handle to Entity Definition</param>
        /// <param name="fldMaps">Deserialized Field Map data</param>
        /// <returns>true if successful, false in case of some error</returns>
        private static void ValidateFieldMapOnCQ(OAdEntityDef entityDef,
                                                 FieldMapsFieldMap[] fldMaps,
                                                 string fieldMapFile)
        {
            string entityName = CQWrapper.GetEntityDefName(entityDef);

            object[] cqFields = (object[])CQWrapper.GetFieldDefNames(entityDef);
            // prepare the list of fields from the current entity type
            Dictionary<string, bool> cqFieldsList = new Dictionary<string, bool>(TFStringComparer.OrdinalIgnoreCase);
            foreach (string cqFld in cqFields)
            {
                cqFieldsList.Add(cqFld, false);
            }

            Display.StartProgressDisplay(UtilityMethods.Format(CQResource.CQ_VALIDATE_FLD_MAP, fieldMapFile));
            try
            {
                StringBuilder invalidFields = new StringBuilder();
                foreach (FieldMapsFieldMap fldMap in fldMaps)
                {
                    if (fldMap.exclude != null &&
                        !TFStringComparer.XmlAttributeValue.Equals(fldMap.exclude, "true") &&
                        !CQConstants.CQInternalFields.ContainsKey(fldMap.from))
                    {
                        // this is to be included in the selected fields for migration
                        if (!cqFieldsList.ContainsKey(fldMap.from))
                        {
                            if (invalidFields.Length > 0)
                            {
                                invalidFields.Append(", ");
                            }
                            invalidFields.Append(fldMap.from);
                        }
                    }
                }

                if (invalidFields.Length > 0)
                {
                    string errMsg = UtilityMethods.Format(CQResource.CQ_FLD_NOT_EXIST, Path.GetFileName(fieldMapFile),
                        invalidFields.ToString(), entityName);

                    PostMigrationReport.WriteIssue(null, null, RepStatus.Failed,
                         ReportIssueType.Critical, String.Empty,
                          entityName, IssueGroup.Witd, errMsg);

                    throw new ConverterException(errMsg);
                }
            }
            finally
            {
                Display.StopProgressDisplay();
                Display.NewLine();
            }
        }   // end of ValidateFieldMapOnCQ

        /// <summary>
        /// Execute the Query and migrate the data
        /// </summary>
        /// <param name="baseEntityName">Base Entity Name</param>
        private void MigrateData(string baseEntityName, string baseEntityWitName)
        {
            Session cqSession = m_cqConnection.GetUserSession();
            OAdQuerydef qryDef = m_cqConnection.QueryDefinition;

            // edit the query and add dbid field
            // dbid is suppose to be unique within a Entity
            CQWrapper.BuildField(qryDef, "dbid");

            // prepare result set
            OAdResultset result = CQWrapper.BuildResultSet(cqSession, qryDef);

            // process records for base entity
            CQEntity baseEntityRecords = m_cqParams.entityRecords[baseEntityName];

            // enable record count before execute so that no of records can be fetched
            CQWrapper.EnableRecordCount(result);

            // execute the query
            CQWrapper.ExecuteResultSet(result);

            int columnCount = CQWrapper.GetResultSetColumnCount(result);

            // lookup for dbid column
            bool dbidExist = false;
            int dbidColumnIndex = 0;
            for (int colIter = 1; colIter <= columnCount; colIter++)
            {
                if (string.Equals(CQWrapper.GetColumnLabel(result, colIter), "dbid", StringComparison.OrdinalIgnoreCase))
                {
                    dbidExist = true;
                    dbidColumnIndex = colIter;
                    break;
                }
            }

            if (!dbidExist)
            {
                // neither query contain dbid nor can be edited to include a new column
                string errMsg = UtilityMethods.Format(CQResource.CQ_NO_DBID_IN_QUERY, m_cqConnection.QueryName,
                    m_convParams.ConfigFile);

                PostMigrationReport.WriteIssue(null, null, RepStatus.Failed, ReportIssueType.Critical,
                    String.Empty, baseEntityName, IssueGroup.Config, errMsg);

                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                throw new ConverterException(errMsg);
            }

            // start the progress thread for updating the progress
            m_progressThread = new Thread(new ThreadStart(CQConverter.UpdateProgress));
            m_progressThread.Name = "Progress";

            try
            {
                // get the work item helper handle
                TotalRecords = CQWrapper.GetRecordCount(result);
                m_progressThread.Start();
                while (CQWrapper.ResultSetMoveNext(result) == CQConstants.SUCCESS)
                {
                    string dbid = (string)CQWrapper.GetColumnValue(result, dbidColumnIndex);
                    // create a CQEntity for that
                    CQEntityRec record = new CQEntityRec(int.Parse(dbid), baseEntityName, m_cqParams);

                    try
                    {
                        // populate and migrate the record and all referenced records
                        RecordsProcessed++;
                        baseEntityRecords.AddRecord(record);
                        if (record.Populate() == false &&
                            m_cqParams.exitOnError == true)
                        {
                            return; // stop processing more records
                        }
                    }
                    catch (ConverterException conEx)
                    {
                        // log the error and continue with next item
                        string errMsg = UtilityMethods.Format(CQResource.CQ_WI_READ_FAILED, dbid, conEx.Message);
                        ReportWorkItemFailure(errMsg, dbid, baseEntityName, baseEntityWitName, m_cqParams.exitOnError);
                        if (m_cqParams.exitOnError == true)
                        {
                            // throw the error back .. should not continue with the current record
                            throw;
                        }
                    }
                }
            }
            finally
            {
                // terminate the progress thread
                m_progressThread.Abort();
                Thread.Sleep(5000); // allow the display thread to stop
            }
        } // end of MigrateData

        /// <summary>
        /// Add the failure to Migration report for the failed work item
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="id"></param>
        /// <param name="ex"></param>
        /// <param name="cqEntityName"></param>
        /// <param name="currituckWitName"></param>
        /// <param name="exitOnError"></param>
        /// <returns></returns>
        internal static void ReportWorkItemFailure(string msg, string id, string cqEntityName, 
                    string currituckWitName, bool exitOnError)
        {
            if (!FailedRecords.ContainsKey(id))
            {
                Logger.Write(LogSource.CQ, TraceLevel.Error, msg);
                PostMigrationReport.WriteIssue(cqEntityName, currituckWitName,
                                               ReportStatisticsStatisicsDetails.MigrationStatus.Failed,
                                               (exitOnError == true ? ReportIssueType.Critical : ReportIssueType.Error),
                                               String.Empty,
                                               id, IssueGroup.Wi, msg);
                Display.DisplayError(msg);
                FailedRecords.Add(id, String.Empty);
            }
        }
        /// <summary>
        /// Get the unique instalation id derived from the Registry
        /// </summary>
        /// <returns>Installation Id</returns>
        private string GetUniqueInstallationId()
        {
            //HKEY_CURRENT_USER\SOFTWARE\Rational Software\ClearQuest\2003.06.00\Core\Databases
            string dbKey = "SOFTWARE\\Rational Software\\ClearQuest";
            string[] cqClients;
            using (RegistryKey cqVersionsKey = Registry.CurrentUser.OpenSubKey(dbKey))
            {
                if (cqVersionsKey == null)
                {
                    string errMsg = UtilityMethods.Format(CQResource.CQ_VER_NOT_SUPPORT,
                                                          CurConResource.Analysis);
                    // no supported cq client is installed
                    Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                    ConverterMain.MigrationReport.WriteIssue(String.Empty,
                             errMsg, string.Empty /* no item */, null, "Config", ReportIssueType.Critical);

                    throw new ConverterException(errMsg);
                }

                cqClients = cqVersionsKey.GetSubKeyNames();
            }

            // check if any of the supported version of CQ client exist
            string server = null;
            string database = null;
            string connection = m_cqConnection.ConnectionName;
            const string connKey = @"{0}\{1}\Core\Databases\{2}\{3}";

            foreach (string clientVer in cqClients)
            {
                // bug#404043 : CQ version check not required
                // if (!CQConstants.SupportedCQVersion.Contains(clientVer))
                //     continue;

                if (string.IsNullOrEmpty(m_cqConnection.ConnectionName))
                {
                    // check for no of user databases
                    const string userDbKey = @"{0}\{1}\Core\Databases";
                    using (RegistryKey cqConnKey = Registry.CurrentUser.OpenSubKey(string.Format(userDbKey, dbKey, clientVer)))
                    {
                        if (cqConnKey != null)
                        {
                            string[] userDbKeys = cqConnKey.GetSubKeyNames();

                            // if there is just one connection entry
                            if (userDbKeys != null && userDbKeys.Length == 1)
                            {
                                // use this connection as the default connection.. as CQ client does
                                connection = userDbKeys[0];
                            }
                        }
                    }
                }

                // look for database name and connection name
                String clientDbKey = UtilityMethods.Format(connKey, dbKey, clientVer, connection, m_cqConnection.UserDbName);

                using (RegistryKey CQRegKey = Registry.CurrentUser.OpenSubKey(clientDbKey))
                {
                    if (CQRegKey == null)
                        continue;

                    // find some client with this userdb and connection
                    server = CQRegKey.GetValue("Server") as string;
                    database = CQRegKey.GetValue("Database") as string;
                    break;
                }
            }

            if (server == null && database == null)
            {
                string errMsg = UtilityMethods.Format(CQResource.CQ_INVALID_CONN,
                                                      CurConResource.Analysis, 
                                                      m_cqConnection.ConnectionName, 
                                                      m_cqConnection.UserDbName, 
                                                      m_convParams.ConfigFile);

                // connection details not found in registry
                Logger.Write(LogSource.CQ, TraceLevel.Error, errMsg);
                ConverterMain.MigrationReport.WriteIssue(String.Empty, errMsg, string.Empty /* no item */,
                    null, "Config", ReportIssueType.Critical);

                throw new ConverterException(errMsg);
            }

            StringBuilder uniqueDb = new StringBuilder();
            if (server != null && server.Length > 0)
            {
                uniqueDb.Append(server);
            }

            if (database != null && database.Length > 0)
            {
                uniqueDb.Append(".");
                uniqueDb.Append(database);
            }

            return uniqueDb.ToString();
        } // end of GetUniqueInstallationId

        /// <summary>
        /// Returns the set of internal field maps which has to be imposed for all the entities..
        /// Used for internal purpose.. in case user already defines some field maps, this will be superseeded
        /// with that
        /// </summary>
        /// <param name="fldMaps"></param>
        /// <returns></returns>
        private static FieldMapsFieldMap[] GetInternalFieldMaps(FieldMaps fldMaps)
        {
            // add the pre defined field mappings for history items
            FieldMapsFieldMap[] newFldMaps = new FieldMapsFieldMap[fldMaps.FieldMap.Length + CQConstants.InternalMap.Count];
            int currindex = 0;

            foreach (KeyValuePair<string, string> map in CQConstants.InternalMap)
            {
                FieldMapsFieldMap newMap = new FieldMapsFieldMap();
                newMap.from = map.Key;
                newMap.to = map.Value;

                if (currindex < CQConstants.NoOfUserFldsInInternalMap)
                {
                    // add the User Map section
                    newMap.ValueMaps = new FieldMapsFieldMapValueMaps();
                    newMap.ValueMaps.refer = CQConstants.UserMapXMLValue;
                    newMap.ValueMaps.defaultValue = Environment.UserName;
                }

                // add from the end
                newFldMaps[newFldMaps.Length - currindex - 1] = newMap;
                currindex++;
            }

            return newFldMaps;
        }

        private static void UpdateProgress()
        {
            string progressMsg = CQResource.CQ_PROGRESS;
            try
            {
                while (true)
                {
                    Display.DisplayMessage(progressMsg, CQConverter.RecordsProcessed, CQConverter.TotalRecords);
                    Thread.Sleep(5000);
                }
            }
            catch (ThreadAbortException)
            {
                Logger.Write(LogSource.CQ, TraceLevel.Error, "Aborting progress thread");
                // update the last status before quitting
                Display.DisplayMessage(progressMsg, CQConverter.RecordsProcessed, CQConverter.TotalRecords);
            }
        }
        #endregion

        #region ANALYZE METHODS
        /// <summary>
        /// Generate User Mappings based on the Clearquest users
        /// Each User mapping generates the same user name in to section also
        /// </summary>
        private void GenerateDefaultUserMaps(string userMapFileName)
        {
            Logger.WritePerf(LogSource.CQ, "Generating Default User Map");
            AdminSession cqAdminSess = m_cqConnection.GetAdminSession();
            OAdUsers users = CQWrapper.GetUsers(cqAdminSess);
            // create a instance of usermap.xml file
            UserMappings userMaps = new UserMappings(userMapFileName);
            for (int userindx = 0; userindx < users.Count; userindx++)
            {
                object userObj = (object)userindx;
                OAdUser aUser = CQWrapper.GetUser(users, ref userObj);
                userMaps.Add(aUser.Name, aUser.Name);
            }
            userMaps.Flush();
            Logger.WritePerf(LogSource.CQ, "Default User Map Generation Done");
        } // end of GenerateDefaultUserMaps()

        #endregion

    } // end of class CQConverter

    // parameters for CQ converter.. to be used internally across fn calls
    internal struct CQConverterParams
    {
        public Session cqSession;
        public List<SchemaMapping> schemaMaps;
        public Dictionary<string, CQEntity> entityRecords;
        public string uniqueInstId;
        public Dictionary<string, bool> allowedEntities;
        public bool exitOnError;
        public VSTSConnection vstsConn;
    };
}
