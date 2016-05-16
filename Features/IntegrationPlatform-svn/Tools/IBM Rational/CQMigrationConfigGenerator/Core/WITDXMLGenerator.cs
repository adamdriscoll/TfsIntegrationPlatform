// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Explores the CQ database and generates Currituck Schema
// Each instance of this class deals with one entity type, which results
// in one WITD xml file.

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using ClearQuestOleServer;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Diagnostics;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using System.Collections;
using Const = Microsoft.TeamFoundation.Converters.WorkItemTracking.Common.CommonConstants;
using Microsoft.TeamFoundation.Converters.Reporting;
#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ
{
    /// <remarks>
    /// This class represents a XML file structure for Currituck  from Clearquest Source.
    /// Acts as a wrapper for generating Currituck Schema from ClearQuest.
    /// </remarks>
    class WITDXMLGenerator
    {
        #region Private Members

        // Name of the xml file for generating Currituck schema
        private string schemaXMLFileName;

        // instance of WITD Schema
        private WITDSchema witdSchema;

        // Name of the xml file for generating FieldMap for 
        // currentCurrituck schema
        private string fieldMapXMLFileName;

        // instance of Field Map
        private WITFieldMappings witdFieldMap;


        // Entity Definition of CQ for which the schema has to be generated
        private OAdEntityDef cqEntityDef;

        // CQ Session handle used to fetch schema information from CQ
        private Session cqSession;

        // instance of Entity Object, used to fetch all the meta data values 
        // which are not available in EntityDef object. Used thorughout the class
        //and will be created in the c'tor
        private OAdEntity cqEntity;

        // list of core field names for which the comment is required to be set
        List<string> fieldsToComment = new List<string>();

        #endregion

        /// <summary>
        /// Parametrized Constructor for instantiating schema object
        /// No other CTors are provided as these values are required to start with
        /// </summary>
        /// <param name="xmlFile">Name of the target schema xml file</param>
        /// <param name="cqSess">Handle to valid CQ session</param>
        /// <param name="entityDef">Handle to valid CQ Entity Definition</param>
        public WITDXMLGenerator(string schemaXmlFile,
                                string fieldMapXmlFile,
                                Session cqSess,
                                OAdEntityDef entityDef,
                                VSTSConnection vstsConn)
        {
            Logger.EnteredMethod(LogSource.CQ, schemaXmlFile, fieldMapXmlFile,
                                    cqSess, entityDef);

            // create instance of WITDFieldMap to store field mappings
            witdFieldMap = new WITFieldMappings();

            // create instance of WITDSchema to store WITD schema
            witdSchema = new WITDSchema();
            witdSchema.SetApplication(Application.Workitemtypeeditor);

            // set the VSTS connection handle for finding the unique fields in VSTS system
            WITDSchema.VstsConn = vstsConn;

            // store file name to be used later to generate xml
            schemaXMLFileName = schemaXmlFile;
            fieldMapXMLFileName = fieldMapXmlFile;

            cqEntityDef = entityDef;
            cqSession = cqSess;
            cqEntity = CQWrapper.BuildEntity(cqSession, CQWrapper.GetEntityDefName(cqEntityDef));

            Logger.ExitingMethod(LogSource.CQ);
        }

        /// <summary>
        /// Generate the Schema XML file based on the parameters provided
        /// </summary>
        public void GenerateSchemaXml()
        {
            CreateWorkItemType();
            GenerateXml();
        }

        /// <summary>
        /// Create the WIT in XML and all the underlying subelements
        /// </summary>
        private void CreateWorkItemType()
        {
            Logger.EnteredMethod(LogSource.CQ);

            WorkItemType wit = new WorkItemType();
            wit.name = CQWrapper.GetEntityDefName(cqEntityDef);

            // set the WIT
            witdSchema.SetWorkItemType(wit);

            wit.FORM = new Form();

            // add Fields to this WIT
            ProcessFields(wit);

            // add Workflow to WIT
            switch (CQWrapper.GetEntityDefType(cqEntityDef))
            {
                case CQConstants.STATE_BASED:
                case CQConstants.STATE_OR_STATELESS:
                    {
                        Logger.Write(LogSource.CQ, TraceLevel.Info, "Workflow is either State Based or Stateless");
                        ProcessWorkflow(wit);
                        break;
                    }

                case CQConstants.STATE_LESS:
                    {
                        // whatever is the type of state we have to generate the workflow information
                        // as that is mandatory as per the XSD file rules.. so a dummy block for Workflow
                        // will be generated
                        Logger.Write(LogSource.CQ, TraceLevel.Info, "Workflow is Stateless. Adding Dummy Workflow");

                        wit.WORKFLOW = new Workflow();
                        CreateDummyWorkflow(wit.WORKFLOW);
                        break;
                    }
            }

            Logger.ExitingMethod(LogSource.CQ);
        }

        /// <summary>
        /// Process and add Fields to the WIT
        /// </summary>
        /// <param name="wit">Handle to WIT to add the fields</param>
        private void ProcessFields(WorkItemType wit)
        {
            Logger.EnteredMethod(LogSource.CQ, wit);

            // add vsts_sourcedb and vsts_sourceid fields
            FieldDefinition vstsIdField = AddInternalFields(wit, CQConstants.IdFieldName, FieldType.Integer);
            FieldDefinition vstsSourceIdField = AddInternalFields(wit, CommonConstants.VSTSSrcIdField, FieldType.String);
            vstsSourceIdField.READONLY = new PlainRule();
            AddInternalFields(wit, CommonConstants.VSTSSrcDbField, FieldType.String);

            // add id and vsts_sourceid in form
            CreateDefaultControl(vstsIdField.name, vstsIdField.refname, FieldType.Integer, wit);
            CreateDefaultControl(CQConstants.SourceFieldLabel, vstsSourceIdField.refname, FieldType.String, wit);
            
            // get all the fields from CQ
            object[] cqFields = (object[])CQWrapper.GetFieldDefNames(cqEntityDef);

            FieldDefinition witField;

            if (cqFields.Length > 0)
            {
                foreach (object ob in cqFields)
                {
                    string fldName = (string)ob;
                    if (CQConstants.InternalFieldTypes.ContainsKey(fldName))
                    {
                        // these are internal clearquest fields
                        // we dont want to migrate these
                        Logger.Write(LogSource.CQ, TraceLevel.Info, "Skipping CQ Internal Field '{0}'", fldName);
                        continue;
                    }

                    int cqFieldType = CQWrapper.GetFieldDefType(cqEntityDef, fldName);

                    string suggestedFldMap = (string)CQConstants.SuggestedMap[fldName];
                    if (suggestedFldMap != null)
                    {
                        // this field name matched to one the suggested mappings to one of the core field
                        // generate the field in schema and also a field map for this..
                        witField = new FieldDefinition();
                        witField.OldFieldName = fldName;
                        witField.name = suggestedFldMap;
                        witField.type = CQConstants.WITFieldTypes[cqFieldType];

                        // use the core field refname and type
                        for (int coreFieldIndex = 0; coreFieldIndex < CQConstants.CurrituckCoreFields.Length; coreFieldIndex++)
                        {
                            string coreFieldName = CQConstants.CurrituckCoreFields[coreFieldIndex].Name;
                            if (TFStringComparer.WorkItemFieldFriendlyName.Equals(coreFieldName, suggestedFldMap))
                            {
                                // use the refname and type from the core fields
                                witField.refname = CQConstants.CurrituckCoreFields[coreFieldIndex].ReferenceName;
                                witField.type = (FieldType)Enum.Parse(typeof(FieldType),
                                    CQConstants.CurrituckCoreFields[coreFieldIndex].FieldType.ToString());
                                break;
                            }
                        }

                        fieldsToComment.Add(witField.name);
                        wit.AddField(witField);

                        FieldMapsFieldMap fldMap = null;
                        // process the field properties to set rules for Required/Read Only and list of values
                        // check if it requires UserMap also
                        if (cqFieldType == CQConstants.FIELD_REFERENCE ||
                            cqFieldType == CQConstants.FIELD_REFERENCE_LIST)
                        {
                            OAdEntityDef refEntity = CQWrapper.GetFieldReferenceEntityDef(cqEntityDef, witField.OldFieldName);
                            if (TFStringComparer.WorkItemType.Equals(CQWrapper.GetEntityDefName(refEntity), "users"))
                            {
                                ProcessUserFieldProperties(cqFieldType, witField, ref fldMap);
                            }
                        }
                        else
                        {
                            ProcessFieldProperties(witField, cqFieldType, false);
                            fldMap = new FieldMapsFieldMap();
                            fldMap.from = witField.OldFieldName;
                            fldMap.to = witField.name;
                            fldMap.exclude = "false";
                        }

                        Logger.Write(LogSource.CQ, TraceLevel.Info, "Using Suggested Field Map {0} to {1}",
                                    witField.OldFieldName, suggestedFldMap.ToString());
                        witdFieldMap.GetFieldMappings().AddFieldMap(fldMap);

                        if (TFStringComparer.WorkItemFieldFriendlyName.Equals(fldMap.to, VSTSConstants.DescriptionField))
                        {
                            CreateDefaultControl(witField.OldFieldName, witField.refname, FieldType.PlainText, wit);
                        }
                        else
                        {
                            CreateDefaultControl(witField.OldFieldName, witField.refname, FieldType.String, wit);
                        }
                        continue;
                    }


                    switch (cqFieldType)
                    {
                        case CQConstants.FIELD_ID:
                        case CQConstants.FIELD_SHORT_STRING:
                        case CQConstants.FIELD_MULTILINE_STRING:
                        case CQConstants.FIELD_INT:
                        case CQConstants.FIELD_DATE_TIME:
                            {
                                Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Migrating Field '{0}'", fldName);
                                witField = new FieldDefinition();
                                witField.OldFieldName = witField.name = fldName;
                                witField.type = CQConstants.WITFieldTypes[cqFieldType];

                                // find the set of allowed values and populate in the xml
                                ProcessFieldProperties(witField, cqFieldType, false);

                                wit.AddField(witField);

                                FieldMapsFieldMap fldMap = new FieldMapsFieldMap();
                                fldMap.from = witField.OldFieldName;
                                fldMap.to = witField.name; // new field name.. if changed
                                fldMap.exclude = "false";

                                // add the field map
                                witdFieldMap.GetFieldMappings().AddFieldMap(fldMap);

                                // add in FORM
                                CreateDefaultControl(witField.OldFieldName, witField.refname, witField.type, wit);

                            }
                            break;

                        case CQConstants.FIELD_REFERENCE_LIST:
                        case CQConstants.FIELD_REFERENCE:
                            {
                                // find the referenced entity name
                                OAdEntityDef refEntity = CQWrapper.GetFieldReferenceEntityDef(cqEntityDef, fldName);
                                if (TFStringComparer.WorkItemType.Equals(CQWrapper.GetEntityDefName(refEntity), "users"))
                                {
                                    // add the refer keyword in the FieldMap file for this field
                                    // and generate the field information for this field..
                                    // as User is a core functionality in currituck..
                                    // handle is in special way
                                    // there are no chances that a "users" field will be of REFERENCE_LIST type..
                                    // in case we see a requirement for REFERENCE_LIST also, add this code there also
                                    Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Migrating User Field '{0}'", fldName);
                                    FieldMapsFieldMap fldMap = null;
                                    witField = new FieldDefinition();
                                    witField.OldFieldName = witField.name = fldName;
                                    ProcessUserFieldProperties(cqFieldType, witField, ref fldMap);

                                    // add in FIELD, FieldMap and FORM
                                    wit.AddField(witField);

                                    // fix for bug# 15769
                                    // set new "To" field name in the field map in case the field name got changed as part of AddField() call
                                    fldMap.to = witField.name;

                                    witdFieldMap.GetFieldMappings().AddFieldMap(fldMap);
                                    CreateDefaultControl(witField.OldFieldName, witField.refname, witField.type, wit);
                                }
                            }
                            break;

                        case CQConstants.FIELD_STATE:
                        case CQConstants.FIELD_ATTACHMENT_LIST:
                        case CQConstants.FIELD_JOURNAL:
                        case CQConstants.FIELD_DBID:
                        case CQConstants.FIELD_STATETYPE:
                        case CQConstants.FIELD_RECORDTYPE:
                            // not migrating these fields as they are CQ internal fields
                            Logger.Write(LogSource.CQ, TraceLevel.Info, "Skipping the Field migration for Internal Field Type '{0}'",
                                                    cqFieldType);
                            break;

                        default:
                            break;
                    } // switch (cqFieldType)
                } // end of foreach (object ob in cqFields)
            }	//if (cqFields.Length > 0)
            else
            {
                Logger.Write(LogSource.CQ, TraceLevel.Warning, "No Fields present in the current Entity Definition '{0}'", wit.name);
            }

            // add all the core fields in the FIELDS section as these fields are being used in the FORM section
            // as per Currituck implementation, any field existing in FORM section has to be in the FIELDS also
            // even if it is a core field
            FieldDefinition coreFldDef = null;
            for (int coreFieldIndex = 0; coreFieldIndex < CQConstants.CurrituckCoreFields.Length; coreFieldIndex++)
            {
                string coreFieldName = CQConstants.CurrituckCoreFields[coreFieldIndex].Name;
                bool fieldExist = false;
                foreach (FieldDefinition fldDef in wit.FIELDS)
                {
                    if (TFStringComparer.WorkItemFieldFriendlyName.Equals(fldDef.name, coreFieldName))
                    {
                        fieldExist = true;
                        break;
                    }
                }

                if (fieldExist == true)
                {
                    // skip this field.. its already added in the FIELDS section
                    continue;
                }

                coreFldDef = new FieldDefinition();
                coreFldDef.name = coreFieldName;
                coreFldDef.refname = CQConstants.CurrituckCoreFields[coreFieldIndex].ReferenceName;
                coreFldDef.type = (FieldType)Enum.Parse(typeof(FieldType),
                    CQConstants.CurrituckCoreFields[coreFieldIndex].FieldType.ToString());

                if (coreFieldIndex < CQConstants.NoOfUserFldsInCoreFields)
                {
                    //add VALIDUSER rule for all user fields
                    coreFldDef.VALIDUSER = new ValidUserRule();
                }

                wit.AddField(coreFldDef);
            }

            int pos;
            ControlType reasonFld = wit.FindFieldInForm(CQConstants.ReasonField, out pos);
            if (reasonFld == null)
            {
                // Bug# 50492: reason field is not yet added .. add it after the State field
                // look for state field position
                wit.FindFieldInForm(CQConstants.StateField, out pos);
                if (pos >= 0)
                {
                    FieldDefinition reasonFldDef = AddInternalFields(wit, CQConstants.ReasonFieldName, FieldType.String);
                    wit.FORM.Layout.WITDItems.Insert(pos+1, CreateDefaultControl(reasonFldDef.name, CQConstants.ReasonField, FieldType.String, null));
                }
            }

            Logger.ExitingMethod(LogSource.CQ);
        } // end of ProcessFields

        /// <summary>
        /// Add internal fields required by the converter.
        /// </summary>
        /// <param name="wit">Work Item Type Name</param>
        /// <param name="fldname">Field Name</param>
        /// <returns>Field Definition handle</returns>
        private static FieldDefinition AddInternalFields(WorkItemType wit, string fldname, FieldType fldType)
        {
            Logger.Write(LogSource.CQ, TraceLevel.Info, "Creating Internal Field {0}", fldname);
            FieldDefinition witField = new FieldDefinition();
            witField.name = fldname;
            witField.type = fldType;
            wit.AddField(witField);
            return witField;
        }

        /// <summary>
        /// Process the field properties and add it in WITD
        /// </summary>
        /// <param name="witField">Handle to Field instance to process the properties</param>
        /// <param name="cqFieldType">ClearQuest field type</param>
        /// <param name="isUserField">Is it a user field</param>
        private void ProcessFieldProperties(FieldDefinition witField, int cqFieldType, bool isUserField)
        {
            Logger.EnteredMethod(LogSource.CQ, witField);
            switch (CQWrapper.GetEntityFieldRequiredness(cqEntity, witField.OldFieldName))
            {
                case CQConstants.MANDATORY:
                    {
                        PlainRule pRule = new PlainRule();
                        witField.REQUIRED = pRule;
                        break;
                    }

                case CQConstants.READONLY:
                    {
                        // PlainRule pRule = new PlainRule();
                        // witField.READONLY = pRule;
                        string roMsg = UtilityMethods.Format(CQResource.CQ_FLD_READONLY_CHANGED, witField.OldFieldName);
                        Logger.Write(LogSource.CQ, TraceLevel.Warning, roMsg);
                        ConverterMain.MigrationReport.WriteIssue(String.Empty,
                                 roMsg, CQWrapper.GetEntityDefName(this.cqEntity), null, IssueGroup.Witd.ToString(), ReportIssueType.Warning);
                        break;
                    }

                case CQConstants.OPTIONAL:
                    // no handling for these behaviors
                    break;

                case CQConstants.USEHOOK:
                    // no handling for these behaviors
                    string hookMsg = UtilityMethods.Format(CQResource.CQ_FLD_SKIP_HOOK, witField.OldFieldName);
                    ConverterMain.MigrationReport.WriteIssue(String.Empty,
                             hookMsg, CQWrapper.GetEntityDefName(this.cqEntity), null, IssueGroup.Witd.ToString(), ReportIssueType.Warning);
                    Logger.Write(LogSource.CQ, TraceLevel.Warning, hookMsg);
                    break;
            }

            if (!isUserField)
            {
                int choiceType = CQWrapper.GetFieldChoiceType(cqEntity, witField.OldFieldName);
                object[] choices = (object[])CQWrapper.GetFieldChoiceList(cqEntity, witField.OldFieldName);
                ListRule choiceList = null;
                if (choices != null && choices.Length > 0)
                {
                    choiceList = new ListRule();
                    foreach (object ob in choices)
                    {
                        ListItem lItem = new ListItem();
                        lItem.value = (string)ob;
                        choiceList.WITDItems.Add(lItem);
                    }

                    // decide whether its ALLOWED or SUGGESTED list
                    if (choiceType == CQConstants.CLOSED_CHOICE &&
                        cqFieldType != CQConstants.FIELD_MULTILINE_STRING)
                    {
                        witField.ALLOWEDVALUES = choiceList;
                    }
                    else
                    {
                        // since currituck does not allow multi value list, for all such fields
                        // set the List Type to Suggested Values (instead of Allowed Values)
                        witField.SUGGESTEDVALUES = choiceList;
                    }

                    // if there is a field containing any of Suggested/Allowed/Prohibited
                    // values, the fieldtype has to be either string or integer..
                    // as per the CQ-Currituck field mappings, we map Multiline String 
                    // to Plain Text.. 
                    // So if it is niether String nor Integer, Move it to String type..
                    if (witField.type != FieldType.String && witField.type != FieldType.Integer)
                    {
                        Logger.Write(LogSource.CQ, TraceLevel.Warning,
                                            "Converting Field {0} of type {1} to String type",
                                            witField.OldFieldName, witField.type);
                        witField.type = FieldType.String;
                    }
                }
            }

            Logger.ExitingMethod(LogSource.CQ);
        } // end of ProcessFieldProperties

        /// <summary>
        /// Add the Workflow information in WIT
        /// </summary>
        /// <param name="wit">Handle to WIT</param>
        private void ProcessWorkflow(WorkItemType wit)
        {
            Logger.EnteredMethod(LogSource.CQ, wit);

            // create a Workflow instance in WIT
            wit.WORKFLOW = new Workflow();

            // add States and Transitions in WIT
            ProcessStatesAndTransitions(wit.WORKFLOW);

            Logger.ExitingMethod(LogSource.CQ);
        }

        /// <summary>
        /// add States and Transitions in WIT
        /// </summary>
        /// <param name="wf">Handle to Workflow item</param>
        private void ProcessStatesAndTransitions(Workflow wf)
        {
            Logger.EnteredMethod(LogSource.CQ, wf);

            // get all states from CQ
            object[] cqStates = (object[])CQWrapper.GetStateDefNames(cqEntityDef);
            if (cqStates.Length > 0)
            {
                string[] states = new string[cqStates.Length];
                Logger.Write(LogSource.CQ, TraceLevel.Verbose, "No of CQ States: [{0}]", cqStates.Length);
                int l = 0;
                foreach (string aState in cqStates)
                {
                    Logger.Write(LogSource.CQ, TraceLevel.Verbose, "CQ State: [{0}]", aState);
                    State witState = new State();
                    witState.value = aState;

                    wf.WITDSTATES.Add(witState);

                    // add the state in states array
                    states[l++] = aState;
                }

                string startState = null;
                object[] cqActions = (object[])CQWrapper.GetActionDefNames(cqEntityDef);
                string submitActionName = null;
                foreach (string action in cqActions)
                {
                    Logger.Write(LogSource.CQ, TraceLevel.Verbose, "CQ Action: [{0}]", action);
                    int actType = CQWrapper.GetActionDefType(cqEntityDef, action);
                    if (actType == CQConstants.ACTION_SUBMIT)
                    {
                        submitActionName = action;
                        startState = CQWrapper.GetActionDestStateName(cqEntityDef, submitActionName);
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Found Start State : From [\"\"] to [{0}]", startState);

                        // add a transition from NULL state to START state
                        Transition witTransition = new Transition();
                        witTransition.from = String.Empty;
                        witTransition.to = startState;

                        // add the default reason as NEW for first transition
                        Reason transReason = new Reason();
                        transReason.value = submitActionName;

                        witTransition.REASONS.DEFAULTREASON = transReason;
                        wf.WITDTRANSITIONS.Add(witTransition);

                        break;
                    }
                }


                // find out all the duplicate and unduplicate actions and duplicate states
                List<string> unduplicateActions = new List<string>();
                List<string> duplicateStates = new List<string>();
                object[] allActions = (object[])CQWrapper.GetActionDefNames(cqEntityDef);
                foreach (string action in allActions)
                {
                    int actionType = CQWrapper.GetActionDefType(cqEntityDef, action);
                    if (actionType == CQConstants.ACTION_DUPLICATE)
                    {
                        string dupActState = CQWrapper.GetActionDestStateName(cqEntityDef, action);
                        if (!string.IsNullOrEmpty(dupActState))
                        {
                            duplicateStates.Add(dupActState);
                        }

                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Duplicate Action : [{0}]", action);
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Duplicate State : [{0}]", dupActState);
                    }
                    else if (actionType == CQConstants.ACTION_UNDUPLICATE)
                    {
                        unduplicateActions.Add(action);
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "UnDuplicate Action : [{0}]", action);
                    }
                }

                // prepare the state transition matrix
                for (int dstState = 0; dstState < states.Length; dstState++)
                {
                    // visit all the source states which can lead to the current destination state
                    for (int srcState = 0; srcState < states.Length; srcState++)
                    {
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Calling DoesTransitionExist Source State: [{0}], Dest State: [{1}]", states[srcState], states[dstState]);
                        object[] transitions = (object[])CQWrapper.DoesTransitionExist(cqEntityDef, states[srcState], states[dstState]);

                        if (transitions != null && transitions.Length > 0)
                        {
                            Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Does Transition returned [{0}] transitions", transitions.Length);
                            Transition witTransition = new Transition();
                            witTransition.from = states[srcState];
                            witTransition.to = states[dstState];

                            // add the reason as the ACTION defined in CQ
                            Reason transReason = new Reason();
                            transReason.value = (string)transitions.GetValue(0);

                            witTransition.REASONS.DEFAULTREASON = transReason;

                            wf.WITDTRANSITIONS.Add(witTransition);
                            Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Adding Transitions [{0}] to [{1}] with Reason [{2}]", states[srcState], states[dstState], transReason.value);
                        }
                        else
                        {
                            Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Does Transition returned No transitions");
                        }
                    }

                    // now for each duplicate action find out all the states that lead to duplicate action
                    // so that back transition could be added
                    for (int dupStateIndex = 0; dupStateIndex < duplicateStates.Count; dupStateIndex++)
                    {
                        string dupState = duplicateStates[dupStateIndex];
                        // here dststate is being used as the source state and the destination state is Duplicate
                        Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Calling DoesTransitionExist Source State: [{0}], Dest State: [{1}]", states[dstState], dupState);
                        object[] transitions = (object[])CQWrapper.DoesTransitionExist(cqEntityDef, states[dstState], dupState);

                        if (transitions != null && transitions.Length > 0)
                        {
                            Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Does Transition returned [{0}] transitions", transitions.Length);
                            bool found = false;
                            // if this transition is not already there..
                            foreach (Transition trans in wf.WITDTRANSITIONS)
                            {
                                if (TFStringComparer.WorkItemStateName.Equals(trans.from, dupState) &&
                                    TFStringComparer.WorkItemStateName.Equals(trans.to, states[dstState]))
                                {
                                    found = true;
                                    // add all the unduplicate actions name as allowed reasons
                                    foreach (string undupActionName in unduplicateActions)
                                    {
                                        // string undupActionName = (string)duplicateActions[dupStateIndex];
                                        if (trans.REASONS.DEFAULTREASON.Equals(undupActionName))
                                        {
                                            // reason already there.. skip this duplicate state and move to next state
                                            continue; // skip this reason and add other reasons
                                        }

                                        // check in other reasons
                                        if (trans.REASONS.REASON == null)
                                        {
                                            // no reasons added yet.. add this
                                            trans.REASONS.WITDREASON.Add(undupActionName);
                                        }
                                        else
                                        {
                                            // check if this reason already there
                                            if (!trans.REASONS.REASON.Contains(undupActionName))
                                            {
                                                trans.REASONS.REASON.Add(undupActionName);
                                            }
                                        }
                                    }
                                    // move to next state
                                    break;
                                } // end of if transition exist
                            }

                            if (!found)
                            {
                                // this back transition is not defined.. create it now
                                // add a unduplicate transition
                                Transition witTransition = new Transition();
                                witTransition.from = dupState;
                                witTransition.to = states[dstState];

                                // add the reason as the ACTION defined in CQ
                                bool isFirst = true;
                                foreach (string undupActionName in unduplicateActions)
                                {
                                    Reason transReason = new Reason();
                                    transReason.value = undupActionName;
                                    if (isFirst)
                                    {
                                        witTransition.REASONS.DEFAULTREASON = transReason;
                                        isFirst = false;
                                    }
                                    else
                                    {
                                        witTransition.REASONS.WITDREASON.Add(transReason);
                                    }
                                }

                                wf.WITDTRANSITIONS.Add(witTransition);
                            }
                        }
                        else
                        {
                            Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Does Transition returned No transitions");
                        }
                    } // end of duplicate action handling
                } // end of preparing state transition matrix for loop
            } //if (cqStates.Length > 0)
            else
            {
                // if the current entity does not have any states the minimal
                // state model has top be created
                Logger.Write(LogSource.CQ, TraceLevel.Verbose, "Creating Dummy Workflow for {0}", schemaXMLFileName);
                CreateDummyWorkflow(wf);
            }

            Logger.ExitingMethod(LogSource.CQ);
        } // end of ProcessStatesAndTransitions

        /// <summary>
        /// Process the CQ properties for USER type field
        /// </summary>
        /// <param name="cqFieldType">ClearQuest field type</param>
        /// <param name="witField">WIT Field handle</param>
        /// <param name="fldMap">Field Map handle (will be populated with Field Map)</param>
        private void ProcessUserFieldProperties(int cqFieldType,
                                                FieldDefinition witField,
                                                ref FieldMapsFieldMap fldMap)
        {
            // set the type explicitly as string for user type field
            witField.type = CQConstants.WITFieldTypes[CQConstants.FIELD_SHORT_STRING];

            fldMap = new FieldMapsFieldMap();
            if (cqFieldType == CQConstants.FIELD_REFERENCE)
            {
                // set the VALIDUSER constraint only if its a single reference
                // for reference list of users, migrate as simple string field bug#399176
                witField.VALIDUSER = new ValidUserRule();
                fldMap.ValueMaps = new FieldMapsFieldMapValueMaps();
                fldMap.ValueMaps.refer = CQConstants.UserMapXMLValue;
            }
            else
            {
                string warningMsg = UtilityMethods.Format(CQResource.CQ_USER_LIST_CHANGED, witField.OldFieldName);
                ConverterMain.MigrationReport.WriteIssue(String.Empty, warningMsg,
                    CQWrapper.GetEntityDefName(this.cqEntity), null, IssueGroup.Witd.ToString(), ReportIssueType.Warning);

                Logger.Write(LogSource.CQ, TraceLevel.Warning, warningMsg);
            }

            // check only for requiredness for User field
            ProcessFieldProperties(witField, cqFieldType, true);

            fldMap.from = witField.OldFieldName;
            fldMap.to = witField.name; // new field name.. if changed
            fldMap.exclude = "false";
        }

        /// <summary>
        /// Add a empty Workflow info
        /// </summary>
        /// <param name="wf"></param>
        private static void CreateDummyWorkflow(Workflow wf)
        {
            State witState = new State();
            witState.value = "Active";

            wf.WITDSTATES.Add(witState);

            // similarly create a dummy transition
            Transition witTransition = new Transition();
            witTransition.from = String.Empty;
            witTransition.to = "Active";

            // add the default reason as NEW for first transition
            Reason transReason = new Reason();
            transReason.value = "New";

            witTransition.REASONS.DEFAULTREASON = transReason;
            wf.WITDTRANSITIONS.Add(witTransition);
        }

        /// <summary>
        /// Generate the XML Schema based on the data processed
        /// </summary>
        private void GenerateXml()
        {
            string entityName = CQWrapper.GetEntityDefName(cqEntityDef);
            string infoMsg = UtilityMethods.Format(CQResource.SchemaCreation, entityName);
            Logger.Write(LogSource.CQ, TraceLevel.Verbose, infoMsg);
            
            Display.StartProgressDisplay(infoMsg);
            try
            {

                witdSchema.GenerateWITDSchema(schemaXMLFileName);
                witdFieldMap.GenerateFieldMappings(fieldMapXMLFileName);

                // bug# 21768.. add the xml comment in witd and field map file
                if (fieldsToComment.Count > 0)
                {
                    string commentMsg = CQResource.CoreFieldComment;
                    // open both document in Xml DOM
                    XmlDocument witdDoc = new XmlDocument();
                    XmlReader witdRdr = new XmlTextReader(schemaXMLFileName);
                    witdDoc.Load(witdRdr);
                    witdRdr.Close();

                    XmlDocument fldMapDoc = new XmlDocument();
                    XmlReader fldMapRdr = new XmlTextReader(fieldMapXMLFileName);
                    fldMapDoc.Load(fldMapRdr);
                    fldMapRdr.Close();

                    XmlNamespaceManager nsm = new XmlNamespaceManager(witdDoc.NameTable);
                    nsm.AddNamespace("CQ", Const.WITDTypesNamespace);

                    XmlNode parentFldNode = witdDoc.SelectSingleNode(
                        String.Format("//CQ:{0}/{1}/{2}", Const.TagWitd, Const.TagWit, Const.TagFields), nsm);
                    XmlNode parentMapNode = fldMapDoc.SelectSingleNode(Const.TagFieldMaps);

                    foreach (string fldName in fieldsToComment)
                    {
                        XmlNode fldNode = parentFldNode.SelectSingleNode(
                            String.Format("/descendant::{0}[@{1} = '{2}']", Const.TagField, Const.TagName, fldName));
                        if (fldNode != null)
                        {
                            XmlComment comment = witdDoc.CreateComment(string.Format(commentMsg, fldName));
                            parentFldNode.InsertBefore(comment, fldNode);
                        }

                        XmlNode mapNode = parentMapNode.SelectSingleNode(
                            String.Format("/descendant::{0}[@{1} = '{2}']", Const.TagFieldMap, Const.TagTo, fldName));
                        if (mapNode != null)
                        {
                            XmlComment comment = fldMapDoc.CreateComment(string.Format(commentMsg, fldName));
                            parentMapNode.InsertBefore(comment, mapNode);
                        }
                    }

                    // write the modified schema file back
                    XmlTextWriter writer = new XmlTextWriter(schemaXMLFileName, null);
                    writer.Formatting = Formatting.Indented;
                    witdDoc.Save(writer);
                    writer.Close();

                    // write the modified field map file back
                    writer = new XmlTextWriter(fieldMapXMLFileName, null);
                    writer.Formatting = Formatting.Indented;
                    fldMapDoc.Save(writer);
                    writer.Close();
                }
            }
            finally
            {
                Display.StopProgressDisplay();
            }
        } // end of Generatexml

        /// <summary>
        /// Create the default control of type "FieldControl" in the FORM
        /// </summary>
        /// <param name="label">Label of the control</param>
        /// <param name="fldName">Actual field name</param>
        /// <param name="cqFieldType">Field Type</param>
        /// <returns></returns>
        private static ControlType CreateDefaultControl(string label,
                                                 string fldName,
                                                 FieldType cqFieldType,
                                                 WorkItemType wit)
        {
            if (CQConstants.FixedFormFields.ContainsKey(fldName))
            {
                // generate the field in FORM only if it is not already there in Fixed Form section
                return null;
            }

            ControlType ctrl = new ControlType();
            ctrl.LabelPosition = new LabelPositionType();
            ctrl.LabelPosition = LabelPositionType.Left;

            ctrl.Label = CQConverterUtil.ValidateXmlString(label);
            ctrl.FieldName = fldName;

            if (cqFieldType != FieldType.PlainText)
            {
                ctrl.Type = Const.GenericControl;
            }
            else
            {
                ctrl.Type = Const.HtmlControl;
            }
            if (wit != null)
            {
                wit.FORM.Layout.WITDItems.Add(ctrl);
            }
            return ctrl;
        }// end of createdefaultcontrol
    } // end of class witdxmlgenerator
}
