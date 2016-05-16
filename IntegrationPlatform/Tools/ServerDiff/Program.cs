// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.TeamFoundation.Migration.BusinessModel;
using Microsoft.TeamFoundation.Migration.Toolkit.ServerDiff;

namespace ServerDiff
{
    public class ServerDiffCommandLine
    {
        /// <summary>
        /// The entry point for the executable. 
        /// Takes the path to a migration configuration file in from the command line and compares
        /// the history of the first VC session in that file.  Logs the differences to the standard out.
        /// </summary>
        /// <param name="args">Command line parameters</param>
        static void Main(string[] args)
        {
            bool noContentComparison = false;
            bool verbose = false;

            if (args.Length == 0)
            {
                LogError(ServerDiffConsoleResources.ServerDiffUsage);
                Environment.Exit(1);
            }

            if (string.Equals(args[0], "vc", StringComparison.InvariantCultureIgnoreCase))
            {
                string leftVersion = null;
                string rightVersion = null;
                Guid sessionGuid = Guid.Empty;

                for (int i = 1; i < args.Length; i++)
                {
                    string arg = RemoveQuotes(args[i]);
                    if ((arg.StartsWith("/s", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/session", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        try
                        {
                            sessionGuid = new Guid(arg.Substring(arg.IndexOf(':') + 1));
                        }
                        catch
                        {
                            LogError(String.Format(CultureInfo.InvariantCulture, ServerDiffConsoleResources.SessionArgIsNotGuid, args[0]));
                            Environment.Exit(1);
                        }
                    }
                    else if ((arg.StartsWith("/l:", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/leftVersion:", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        leftVersion = arg.Substring(arg.IndexOf(':') + 1);
                    }
                    else if ((arg.StartsWith("/r:", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/rightVersion:", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        rightVersion = arg.Substring(arg.IndexOf(':') + 1);
                    }
                    else if ((arg.Equals("/n", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.Equals("/noContentComparison ", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        noContentComparison = true;
                    }
                    else if ((arg.Equals("/v", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.Equals("/verbose", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        verbose = true;
                    }
                    else
                    {
                        LogError(ServerDiffConsoleResources.ServerDiffUsage);
                        Environment.Exit(1);
                    }
                }

                try
                {

                    ServerDiffEngine diffEngine = new ServerDiffEngine(sessionGuid, noContentComparison, verbose, SessionTypeEnum.VersionControl);
                    VCDiffComparer vcDiffComparer = new VCDiffComparer(diffEngine);
                    diffEngine.RegisterDiffComparer(vcDiffComparer);
                    if (diffEngine.VerifyContentsMatch(leftVersion, rightVersion))
                    {
                        diffEngine.LogResult(ServerDiffResources.AllContentsMatch);
                    }
                    else
                    {
                        diffEngine.LogResult(ServerDiffResources.ContentsDoNotMatch);
                    }
                }
                catch (Exception e)
                {
                    LogError(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ExceptionRunningServerDiff,
                        verbose ? e.ToString() : e.Message));
                }
            }
            else if (string.Equals(args[0], "wit", StringComparison.InvariantCultureIgnoreCase))
            {
                string forceSyncFile = null;
                string leftQueryCondition = null;
                string rightQueryCondition = null;
                HashSet<string> leftFieldNamesToIgnore = new HashSet<string>();
                HashSet<string> rightFieldNamesToIgnore = new HashSet<string>();
                Guid sessionGuid = Guid.Empty;

                for (int i = 1; i < args.Length; i++)
                {
                    string arg = RemoveQuotes(args[i]);
                    if ((arg.StartsWith("/s", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/session", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        try
                        {
                            sessionGuid = new Guid(arg.Substring(arg.IndexOf(':') + 1));
                        }
                        catch
                        {
                            LogError(String.Format(CultureInfo.InvariantCulture, ServerDiffConsoleResources.SessionArgIsNotGuid, args[0]));
                            Environment.Exit(1);
                        }
                    }
                    else if ((arg.StartsWith("/f:", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/ForceSyncFile:", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        forceSyncFile = arg.Substring(arg.IndexOf(':') + 1);
                        if (!forceSyncFile.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            LogError(ServerDiffConsoleResources.ForceSyncFileRequiresCsvExtension);
                            Environment.Exit(1);
                        }
                    }
                    else if ((arg.StartsWith("/l:", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/leftQueryCondition:", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        leftQueryCondition = arg.Substring(arg.IndexOf(':') + 1);
                    }
                    else if ((arg.StartsWith("/r:", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/rightQueryCondition:", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        rightQueryCondition = arg.Substring(arg.IndexOf(':') + 1);
                    }

                    else if ((arg.StartsWith("/il:", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/IgnoreLeftFields:", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        string [] leftFieldNames = arg.Substring(arg.IndexOf(':') + 1).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string fieldName in leftFieldNames)
                        {
                            leftFieldNamesToIgnore.Add(fieldName.Trim());
                        }
                    }
                    else if ((arg.StartsWith("/ir:", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.StartsWith("/IgnoreRightFields:", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        string[] rightFieldNames = arg.Substring(arg.IndexOf(':') + 1).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string fieldName in rightFieldNames)
                        {
                            rightFieldNamesToIgnore.Add(fieldName.Trim());
                        }
                    }
                    else if ((arg.Equals("/n", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.Equals("/noContentComparison ", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        noContentComparison = true;
                    }
                    else if ((arg.Equals("/v", StringComparison.InvariantCultureIgnoreCase)
                        || (arg.Equals("/verbose", StringComparison.InvariantCultureIgnoreCase))))
                    {
                        verbose = true;
                    }
                    else
                    {
                        LogError(ServerDiffConsoleResources.ServerDiffUsage);
                        Environment.Exit(1);
                    }
                }

                try
                {
                    ServerDiffEngine diffEngine =
                        new ServerDiffEngine(sessionGuid, noContentComparison, verbose, SessionTypeEnum.WorkItemTracking);
                    WITDiffComparer witDiffComparer = new WITDiffComparer(diffEngine);
                    witDiffComparer.ForceSyncFile = forceSyncFile;
                    witDiffComparer.LeftFieldNamesToIgnore = leftFieldNamesToIgnore;
                    witDiffComparer.RightFieldNamesToIgnore = rightFieldNamesToIgnore;
                    diffEngine.RegisterDiffComparer(witDiffComparer);
                    if (diffEngine.VerifyContentsMatch(leftQueryCondition, rightQueryCondition))
                    {
                        diffEngine.LogResult(ServerDiffResources.AllContentsMatch);
                    }
                    else
                    {
                        diffEngine.LogResult(ServerDiffResources.ContentsDoNotMatch);
                    }
                }
                catch (Exception e)
                {
                    LogError(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ExceptionRunningServerDiff,
                        verbose ? e.ToString() : e.Message));
                }
            }
            else
            {
                LogError(ServerDiffConsoleResources.ServerDiffUsage);
                Environment.Exit(1);
            }
        }

        private static string RemoveQuotes(string inString)
        {
            if (inString.StartsWith("\"", StringComparison.Ordinal))
            {
                inString = inString.Substring(1);
            }
            
            if (inString.EndsWith("\"", StringComparison.Ordinal))
            {
                inString = inString.Substring(0, inString.Length-1);
            }

            return inString;
        }

        private static void LogError(string message)
        {
            Console.WriteLine(String.Format(CultureInfo.InvariantCulture, ServerDiffResources.ServerDiffError, message));
        }
    }

}