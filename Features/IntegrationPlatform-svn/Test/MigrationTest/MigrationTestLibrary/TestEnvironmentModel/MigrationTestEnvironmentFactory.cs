// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace MigrationTestLibrary
{
    public class MigrationTestEnvironmentFactory
    {
        // constants
        public const string cTestEnvironment = "MigrationTestEnvironment";
        public const string cTestEnvironmentFile = "MigrationTestEnvironment.xml";

        public static MigrationTestEnvironment CreateMigrationTestEnvironment(string testName)
        {
            MigrationTestEnvironment env = null;

            string fileName = Environment.GetEnvironmentVariable(cTestEnvironment);
            if (String.IsNullOrEmpty(fileName))
            {
                fileName = cTestEnvironmentFile;
            }

            try
            {
                Trace.TraceInformation("Deserializing {0}", Path.GetFullPath(fileName));
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(MigrationTestEnvironment));
                    env = serializer.Deserialize(fs) as MigrationTestEnvironment;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to load {0}", fileName);
                throw ex;
            }

            if (env != null)
            {
                env.TestName = testName;
            }

            return env;
        }

        public static MigrationTestEnvironment CreateMigrationTestEnvironment(string testName, string configFileName)
        {
            MigrationTestEnvironment env = null;

            try
            {
                using (FileStream fs = new FileStream(configFileName, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(MigrationTestEnvironment));
                    env = serializer.Deserialize(fs) as MigrationTestEnvironment;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to load {0}", configFileName);
                throw ex;
            }

            if (env != null)
            {
                env.TestName = testName;
            }

            return env;
        }
    }
}
