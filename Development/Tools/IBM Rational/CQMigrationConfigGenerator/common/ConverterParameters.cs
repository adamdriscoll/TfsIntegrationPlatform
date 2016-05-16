// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using System.Diagnostics;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.Reporting;

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.Common
{
    internal class SchemaMapping
    {
        //VSTS Connection instance
        public string entity;
        public string WIT;
        public string WITDFile;
        public string fieldMapFile;
        public string schemaMapFile;

        // shall be initialized by individual converter
        // and would be interpreted..
        // reason to keep it as object is that we dont want
        // any dependency on target side data structures
        public object vstsHelper;
    }

    public class ConverterParameters
    {
        // command line arguments
        private string schemaMapFile;
        private string configFile;

        // populated using SchemaMap.xml file
        private List<SchemaMapping> schemaMaps;

        // populated using UserMap.xml file (from SchemaMap.xml)
        private UserMappings userMappings;
        private string userMapFile;

        // xml nodes in config.xml for source and target
        private XmlNode sourceConfig;
        private XmlNode targetConfig;

        private bool exitOnError;
        private EditSourceItemOption editSourceItem;
        private string outputDirectory;

        private XmlNode ladyBugConfig;

        private XmlNode summaryMail;

        public XmlNode SummaryMail
        {
            get { return summaryMail; }
            set { summaryMail = value; }
        }

        public XmlNode LadyBugConfig
        {
            get { return ladyBugConfig; }
            set { ladyBugConfig = value; }
        }

        internal string SchemaMapFile
        {
            get { return schemaMapFile; }
            set { schemaMapFile = value; }
        }

        internal string ConfigFile
        {
            get { return configFile; }
            set { configFile = value; }
        }

        internal List<SchemaMapping> SchemaMaps
        {
            get { return schemaMaps; }
        }

        internal UserMappings UsersMappings
        {
            get { return userMappings; }
        }

        internal string UserMapFile
        {
            get { return userMapFile; }
        }

        internal XmlNode SourceConfig
        {
            get { return sourceConfig; }
            set { sourceConfig = value; }
        }

        internal XmlNode TargetConfig
        {
            get { return targetConfig; }
            set { targetConfig = value; }
        }

        internal string OutputDirectory
        {
            get { return outputDirectory; }
            set { outputDirectory = value; }
        }

        internal bool ExitOnError
        {
            get { return exitOnError; }
            set { exitOnError = value; }
        }

        //internal EditSourceItemOption EditSourceItem
        //{
        //    get { return editSourceItem; }
        //    set { editSourceItem = value; }
        //}

        internal ConverterParameters()
        {
            schemaMaps = new List<SchemaMapping>();
        }

    } // end of class ConverterParams

    internal enum EditSourceItemOption
    {
        NoChange,
        AddComment,
        AddCommentAndResolve
    }
}
