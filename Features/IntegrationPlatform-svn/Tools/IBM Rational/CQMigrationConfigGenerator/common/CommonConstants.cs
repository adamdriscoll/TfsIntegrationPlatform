// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

#region Using directives

using System;
using System.Text;
using System.IO;
using Microsoft.TeamFoundation.Converters.Utility;

#endregion

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.Common
{
    static class CommonConstants
    {
        // report file names
        internal const string CQPreMigrationReportName = "CQAnalysisReport.xml";
        internal const string CQPostMigrationReportName = "CQMigrationReport.xml";
        internal const string PSPreMigrationReportName = "PSAnalysisReport.xml";
        internal const string PSPostMigrationReportName = "PSMigrationReport.xml";
        internal const string CQConverterXsdFile = "CQConverter.common.CQConverterConfig.xsd";
        internal const string PSConverterXsdFile = "CQConverter.common.PSConverterConfig.xsd";
        internal const string WITDXsdFile = "CQConverter.common.WorkItemTypeDefinition.xsd";
        internal const string WorkItemConfigXsdFile = "CQConverter.common.WorkItemConverterConfig.xsd";

        // CQConverter required internal field names
        internal const string VSTSSrcIdField = "vsts sourceid";
        internal const string VSTSSrcDbField = "vsts sourcedb";
        
        // WorkItemTypeDefinition Namespace
        internal const string WITDTypesNamespace = "http://schemas.microsoft.com/VisualStudio/2005/workitemtracking/typedef";
        internal const string TagWitd = "WITD";
        internal const string TagWit = "WORKITEMTYPE";
        internal const string TagFields = "FIELDS";
        internal const string TagField = "FIELD";
        internal const string TagFieldMaps = "FieldMaps";
        internal const string TagFieldMap = "FieldMap";
        internal const string TagName = "name";
        internal const string TagTo = "to";

        // Control Names
        internal const string GenericControl = "FieldControl";
        internal const string HtmlControl = "HtmlFieldControl";

        private static StringBuilder m_unresolvedUsers;
        internal static StringBuilder UnresolvedUsers
        {
            get
            {
                if (m_unresolvedUsers == null)
                {
                    m_unresolvedUsers = new StringBuilder();
                }
                return m_unresolvedUsers;
            }
        }

        // date time format
        private static string DateFormat = "{0:d4}-{1:d2}-{2:d2}T{3:d2}:{4:d2}:{5:d2}.{6:d3}";
        internal static string ConvertDateToString(DateTime dt)
        {
            return string.Format(DateFormat, dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }

        private static string mTempPath;
        internal static string TempPath
        {
            get
            {
                if (mTempPath == null)
                {
                    mTempPath = Environment.GetEnvironmentVariable("TEMP");
                    if (string.IsNullOrEmpty(mTempPath))
                    {
                        // no TEMP env var set.. generate the file in current directory
                        mTempPath = string.Empty;
                    }
                }
                return mTempPath;
            }
        }

        // setting output directory for generating files in specified dir
        private static string OutputDirectory;
        public static void SetOutputDirectory(string dirName, string configFile)
        {
            try
            {
                UtilityMethods.CreateDirectory(dirName);
            }
            catch (ConverterException exception)
            {
                // unable to create directory
                string errMsg = UtilityMethods.Format(CurConResource.OutputDirCreationFailed, exception.Message, configFile);
                throw new ConverterException(errMsg);
            }
            OutputDirectory = dirName;
        }
        
        public static string GetFileNameWithPath(string fileName)
        {
            if (OutputDirectory != null)
            {
                return Path.Combine(OutputDirectory, fileName);
            }
            else
            {
                return fileName;
            }
        }


        // only for debugging
#if DEBUG
        internal static int NoOfAttachments = 0;
        internal static int NoOfHistory = 0;
        internal static int NoOfLinks = 0;
        internal static long TotalAttachmentSize = 0;
        internal static int NoOfBugs = 0;
#endif
    }

    enum CommonErrorNumbers
    {
        ErrorInitConverterParams = 2001,
        // FileDoesnotExist,
        SchemaMapNotRequired,
        SchemaMapMandatory,
        InvalidCommand,
        NoSchemaMap,
        NoUserMap,
        ControlKeyPressed,
        InvalidArgument,
        MissingArgument,
        UnhandledExceptionInInput,
        ValueCannotBeNull,
        InvalidSourceFieldMap,
        InvalidTargetFieldMap,
        MultipleSourceEntity,
        MultipleTargetEntity,
        NullFromField,
        SchemaMapFileValidationFailed
    }
    
    internal enum ConverterType
    {
        PS,
        CQ,
        PSDRT
    }
}
