// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Diagnostics;

namespace WitdDiff
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 6)
            {
                Console.WriteLine("Usage :");
                Console.WriteLine("\t WitdDiff.exe sourceServerName sourceTeamProjectName sourceWitName targetServerName targetTeamProjectName targetWitName");
                Console.WriteLine("\t Ex) WitdDiff.exe server1 tp1 Bug1 server2 tp2 Bug2");
                Environment.Exit(0);
            }

            string sourceServer = args[0];
            string sourceTP = args[1];
            string sourceWitName = args[2];
            string targetServer = args[3];
            string targetTP = args[4];
            string targetWitName = args[5];

            WorkItemType srcWit = GetWorkItemType(sourceServer, sourceTP, sourceWitName);
            WorkItemType tarWit = GetWorkItemType(targetServer, targetTP, targetWitName);

            SymDiff<string> witFieldDiff = GetWitFieldDiff(srcWit, tarWit);

            PrintFieldDiff(witFieldDiff, srcWit, tarWit);

            PrintRequiredFields(srcWit);
            PrintRequiredFields(tarWit);
        }

        private static void PrintFieldDiff(SymDiff<string> witFieldDiff, WorkItemType srcWit, WorkItemType tarWit)
        {
            Console.WriteLine("======== Left only fields ========");
            Console.WriteLine("Server Name: " + srcWit.Store.TeamFoundationServer.Name);
            Console.WriteLine("WIT Name: " + srcWit.Name);
            Console.WriteLine();
            foreach (string field in witFieldDiff.LeftOnly)
            {
                Console.WriteLine(field);
            }

            Console.WriteLine();
            Console.WriteLine("======== Right only fields ========");
            Console.WriteLine("Server Name: " + tarWit.Store.TeamFoundationServer.Name);
            Console.WriteLine("WIT Name: " + tarWit.Name);
            Console.WriteLine();
            foreach (string field in witFieldDiff.RightOnly)
            {
                Console.WriteLine(field);
            }
        }
 
        private static SymDiff<string> GetWitFieldDiff(WorkItemType srcWit, WorkItemType tarWit)
        {
            List<string> srcWitFields = new List<string>();
            foreach (FieldDefinition fd in srcWit.FieldDefinitions)
            {
                srcWitFields.Add(fd.ReferenceName);
            }
            srcWitFields.Sort();

            List<string> tarWitFields = new List<string>();
            foreach (FieldDefinition fd in tarWit.FieldDefinitions)
            {
                tarWitFields.Add(fd.ReferenceName);
            }
            tarWitFields.Sort();

            SymDiff<string> fieldDiff = new SymDiff<string>(
                srcWitFields.ToArray(),
                tarWitFields.ToArray(),
                StringComparer.InvariantCulture);

            return fieldDiff;
        }

        private static void PrintRequiredFields(WorkItemType wit)
        {
            Console.WriteLine("======== Required fields ========");
            Console.WriteLine("Server Name: " + wit.Store.TeamFoundationServer.Name);
            Console.WriteLine("WIT Name: " + wit.Name);
            Console.WriteLine();

            WorkItem wi = new WorkItem(wit);
            foreach (Field f in wi.Fields)
            {
                if (f.IsRequired)
                {
                    Console.WriteLine(string.Format("Field: {0} {1}", f.ReferenceName, f.Name));
                    PrintFieldAllowedValues(f);
                    Console.WriteLine();
                }
            }
        }

        private static void PrintFieldAllowedValues(Field f)
        {
            Console.Write("\tAllowed Values: ");
            foreach (var v in f.FieldDefinition.AllowedValues)
            {
                Console.Write(v.ToString() + ", ");
            }
        }

        private static WorkItemType GetWorkItemType(string serverName, string teamProjectName, string witName)
        {
            string serverUrl = string.Format("http://{0}:8080", serverName);

            TeamFoundationServer tfsServer = TeamFoundationServerFactory.GetServer(serverUrl);
            WorkItemStore wis = (WorkItemStore)tfsServer.GetService(typeof(WorkItemStore));

            Project project = wis.Projects[teamProjectName];

            Debug.Assert(project != null);

            return project.WorkItemTypes[witName];
        }
    }
}
