// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// Description: Main program that triggers the migration for 
//              all Currituck Converters

#region Using directives
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using Microsoft.TeamFoundation.Converters.Utility;
using Microsoft.TeamFoundation.Converters.WorkItemTracking.Common;
using Microsoft.TeamFoundation.Converters.Reporting;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Services.Protocols;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
#endregion

//*************************************************************************************************
// Name: WorkItemTracker namespace
//*************************************************************************************************

namespace Microsoft.TeamFoundation.Converters.WorkItemTracking.Common
{
    internal static class ConverterMain
    {
        private static Assembly CurrentAssembly = Assembly.GetExecutingAssembly(); // current assembly handle
        internal static ConverterParameters convParams = new ConverterParameters();
        private static Hashtable m_sourceIdToTypeMaps;
        private static IWorkItemConverter converter;
        private static ConverterType CurrentConverter;      // CQ
        private static string CurrentConverterSourceName;   // name of the converter
        private static bool m_UnexpectedTermination;
        private static bool m_IsEndConveterCalled;
        private static object m_EndConverterLock = new object();

        internal static Report MigrationReport;

        static ConverterMain()
        {
            CurrentConverter = ConverterType.CQ;
            CurrentConverterSourceName = "IBM (R) ClearQuest"; // Do not localize
        }

        private static Hashtable SourceIdToTypeMaps
        {
            get
            {
                if (m_sourceIdToTypeMaps == null)
                {
                    m_sourceIdToTypeMaps = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    m_sourceIdToTypeMaps.Add("ClearQuest", "Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ.CQConverter");
                    m_sourceIdToTypeMaps.Add("ClearQuestVerifier", "Microsoft.TeamFoundation.Converters.WorkItemTracking.CQ.CQVerifier");
                }

                return m_sourceIdToTypeMaps;
            }
        }


        /// <summary>
        /// Converter program for all Currituck converters
        /// </summary>
        [MTAThread]
        public static void Main(String[] args)
        {
            ConverterNativeMethods.HandlerRoutine handlerRoutine = null;
            try
            {
                Display.InitDisplay(ConverterMain.GetErrorsCount);
                ThreadManager.AddThreadToManagerList(Thread.CurrentThread);

                handlerRoutine = InitializeHandlers();

                Logger.WritePerf(LogSource.Common, "Beginning Converter");

                Debug.Assert(!string.IsNullOrEmpty(CurrentConverterSourceName));
                CommandLineParser.DisplayCopyRightMessage();

                CommandLineParser.AddArgumentDetails(new ArgumentDetails[]
                    {
                        new ArgumentDetails("migrationsettings", "m", HandleConfig, true, false),
                        //new ArgumentDetails("schemamap", "s", HandleSchemaMap, false, false),
                        //new ArgumentDetails("command", "c", HandleMode, true, false),
                        new ArgumentDetails("?", "?", DisplayHelp, false, true),
                        new ArgumentDetails("help", "h", DisplayHelp, false, true),
                    }
                );

                if (!CommandLineParser.Parse(args) || !ValidateArgumentsDependency())
                {
                    return;
                }

                // Read the configuration schema from the resources
                XmlDocument xmldoc = UtilityMethods.ValidateXmlFile(convParams.ConfigFile, CommonConstants.WorkItemConfigXsdFile);

                bool exitOnError = false;
                string value = GetValueForNode(xmldoc, "ExitOnError");
                if (value != null)
                {
                    if (!Boolean.TryParse(value, out exitOnError))
                    {
                        string errMsg = UtilityMethods.Format(CommonResource.InvalidBoolean, value, "ExitOnError", convParams.ConfigFile);
                        throw new ConverterException(errMsg);
                    }
                }

                EditSourceItemOption editSourceItem = EditSourceItemOption.NoChange; // default
                value = GetValueForNode(xmldoc, "EditSourceItem");
                if (value != null)
                {
                    editSourceItem = (EditSourceItemOption)Enum.Parse(typeof(EditSourceItemOption), value);
                }

                string outputDir = GetValueForNode(xmldoc, "OutputDirectory");

                // Get the configuration for PS and make a connection
                XmlNodeList xmlNodes = xmldoc.GetElementsByTagName("Source");
                XmlNode sourceNode = xmlNodes[0];

                // Get the configuration for VSTS and make a connection
                XmlNodeList targetNode = xmldoc.GetElementsByTagName("VSTS");

                //LADYBUG RELATED......
                // Get the configuration for VSTS and make a connection
                XmlNodeList ladyBugProcessingNode = xmldoc.GetElementsByTagName("LadyBugProcessing");
                XmlNodeList summaryMailNode = xmldoc.GetElementsByTagName("SendSummaryEmail");

                // Get the handler class for this [work item] source
                XmlAttribute attr = sourceNode.Attributes["id"];
                string productType = (string)SourceIdToTypeMaps[attr.Value];
                Type handlerType = null;
                if (productType != null)
                {
                    handlerType = Type.GetType(productType);
                }

                // If productType is null, handlerType will also be null.
                if (handlerType == null)
                {
                    Logger.Write(LogSource.Common, TraceLevel.Error, "Product Id {0} not registered", attr.Value);
                    throw new ConverterException(
                        UtilityMethods.Format(CurConResource.InvalidSourceId,
                            attr.Value, convParams.ConfigFile));
                }

                // Create the converter/handler for this data source
                converter = (IWorkItemConverter)Activator.CreateInstance(handlerType);

                convParams.SourceConfig = sourceNode;
                convParams.TargetConfig = targetNode[0];
                convParams.ExitOnError = exitOnError;
                convParams.OutputDirectory = outputDir;

                #region before calling initialize, first initialize the report

                MigrationReport = new Report(CommonConstants.CQPreMigrationReportName);
                MigrationReport.UserInput.Options = new string[2];
                MigrationReport.UserInput.Options[0] = CurConResource.AnalyzeCommand;
                MigrationReport.UserInput.Options[1] = 
                    UtilityMethods.Format(CurConResource.ConfigFile, convParams.ConfigFile);

                #endregion
                

                ReportConverter convType = CurrentConverter == ConverterType.CQ?
                    ReportConverter.CQConverter : ReportConverter.PSConverter;
                MigrationReport.StartReporting(convType);

                // now the reporting is initialized.. put command line options
                StringBuilder cmdArgs = new StringBuilder(CurrentAssembly.GetName().Name + ".exe ");
                foreach (string arg in args)
                {
                    cmdArgs.Append(arg);
                    cmdArgs.Append(" ");
                }

                MigrationReport.UserInput.CommandLine = cmdArgs.ToString();

                // Begin the conversion operation
                // Initialize the converter handler object
                converter.Initialize(convParams);
                converter.Convert();
            }
            catch (ConverterException cEx)
            {
                HandleTopException(cEx);
            }
            catch (InvalidOperationException cEx)
            {
                HandleTopException(cEx);
            }
            catch (SerializationException cEx)
            {
                HandleTopException(cEx);
            }
            catch (ThreadInterruptedException cEx)
            {
                HandleTopException(cEx);
            }
            catch (COMException cEx)
            {
                HandleTopException(cEx);
            }
            catch (SoapException cEx)
            {
                HandleTopException(cEx);
            }
            catch (ApplicationException ex)
            {
                // add in the migration report
                HandleTopException(ex);
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            finally
            {
                EndConverter();
            }
        }

        private static void HandleTopException(Exception ex)
        {
            if (m_UnexpectedTermination)
                return;

            Logger.WriteException(LogSource.Common, ex);
            if (MigrationReport != null && !MigrationReport.Statistics.HasCriticalError)
            {
                MigrationReport.WriteIssue("Miscellaneous", ReportIssueType.Critical, ex.Message);
            }

            Display.DisplayError(ex.Message);
        }

        private static void GetErrorsCount(out long numberOfErrors)
        {
            if (MigrationReport != null &&
                MigrationReport.Statistics != null)
            {
                numberOfErrors = MigrationReport.Statistics.NumberOfErrors;
                if (0 == numberOfErrors && MigrationReport.Statistics.HasCriticalError)
                {
                    numberOfErrors = 1;
                }
            }
            else
            {
                // since Migration report not yet initialized
                // assume the current error as the only error
                numberOfErrors = 1;
            }
        }

        /// <summary>
        /// Performs end converter job in this section
        /// </summary>
        private static void EndConverter()
        {
            lock (m_EndConverterLock)
            {
                if (m_IsEndConveterCalled)
                {
                    return;
                }
                else
                {
                    m_IsEndConveterCalled = true;
                }
            }

            Logger.WritePerf(LogSource.Common, "Ending Converter");
#if DEBUG
            Logger.Write(LogSource.Common, TraceLevel.Warning, "No of Bugs: {0}", CommonConstants.NoOfBugs);
            Logger.Write(LogSource.Common, TraceLevel.Warning, "No of Attachments: {0}", CommonConstants.NoOfAttachments);
            Logger.Write(LogSource.Common, TraceLevel.Warning, "Size of Attachments: {0}", CommonConstants.TotalAttachmentSize);
            Logger.Write(LogSource.Common, TraceLevel.Warning, "No of Histories  : {0}", CommonConstants.NoOfHistory);
            Logger.Write(LogSource.Common, TraceLevel.Warning, "No of Links      : {0}", CommonConstants.NoOfLinks);
#endif
            if (CommonConstants.UnresolvedUsers.Length > 2)
            {
                string unresolvedUsers = (CommonConstants.UnresolvedUsers.ToString()).Substring(0, CommonConstants.UnresolvedUsers.Length - 2);
                // there are some unresolved users during migration.. report them in log file and migration report
                Logger.Write(LogSource.Common, TraceLevel.Warning, "The following users appears in the work items as is because the converter is not able to resolve the display name of these users.{0}{1}",
                    Environment.NewLine, unresolvedUsers);
                if (MigrationReport != null)
                {
                    string warningMsg = UtilityMethods.Format(CurConResource.UnresolvedUsers, unresolvedUsers);
                    MigrationReport.WriteIssue(string.Empty, warningMsg,
                        string.Empty, string.Empty, "Misc", ReportIssueType.Warning, null);
                }
            }
            // set the migration status to error/warning/success
            // if there is some fatal/critical error, the status is default set to Incomplete
            if (MigrationReport != null)
            {
                string completionMsg = String.Empty;
                string reportFile = String.Empty;
                long noOfError = 0;
                GetErrorsCount(out noOfError);
                
                reportFile = UtilityMethods.Format(CurConResource.AnalysisReportFile, MigrationReport.ReportFileName);
                if (MigrationReport.Statistics.HasCriticalError || noOfError > 0)
                {
                    completionMsg = UtilityMethods.Format(CurConResource.AnalysisWithErrors,
                        MigrationReport.Statistics.NumberOfWarnings, noOfError);
                    MigrationReport.Summary.Status = CommonResource.AnalysisFailed;
                }
                else if (MigrationReport.Statistics.NumberOfWarnings > 0)
                {
                    completionMsg = UtilityMethods.Format(CurConResource.AnalysisWithWarnings,
                        MigrationReport.Statistics.NumberOfWarnings);
                    MigrationReport.Summary.Status = CommonResource.AnalysisCompleted;
                }
                else
                {
                    completionMsg = CurConResource.AnalysisSuccessful;
                    MigrationReport.Summary.Status = CommonResource.AnalysisCompleted;
                }

                Display.DisplayMessage(completionMsg);
                Display.DisplayMessage(reportFile);

                Logger.WritePerf(LogSource.Common, "Generating Migration Report");
                MigrationReport.EndReporting(true);
                Logger.WritePerf(LogSource.Common, "Migration Report Generated");
            }

            if (converter != null)
            {
                converter.CleanUp();
            }

            Display.EndDisplay();
        }

        /// <summary>
        /// Handler for unhandled exceptions (Used in case there are multiple
        /// threads doing the conversion parallely!
        /// </summary>
        private static void OnUnhandledException(Object source, UnhandledExceptionEventArgs args)
        {
            Exception exception = (Exception)args.ExceptionObject;
            Logger.WriteException(LogSource.Common, exception);
            string errMsg = UtilityMethods.Format(CommonResource.UnhandledException, exception.Message);
            CleanUpUnexpectedAbort(errMsg);
        }

        /// <summary>
        /// Initializes unhandledexception and controlc handlers
        /// </summary>
        private static ConverterNativeMethods.HandlerRoutine InitializeHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(OnUnhandledException);
            Console.CancelKeyPress +=
                new ConsoleCancelEventHandler(OnCancelKeyPress);
            ConverterNativeMethods.HandlerRoutine handler =
                new ConverterNativeMethods.HandlerRoutine(ConsoleCtrlCheck);

            if (!ConverterNativeMethods.SetConsoleCtrlHandler(handler,
                true/*install handler*/))
            {
                //If installation of handler fails
                Debug.Fail("SetConsoleCtrlHandler call failed"); //How to call GetLastError?
                int errorCode = ConverterNativeMethods.GetLastWin32Error();
                Logger.Write(LogSource.Common, TraceLevel.Warning,
                    "SetConsoleCtrlHandler call failed. Error code:: {0}", errorCode);
            }

            return handler;
        }


        /// <summary>
        /// Control message handler
        /// </summary>
        /// <param name="ctrlType"> Control message type</param>
        /// <returns></returns>
        private static bool ConsoleCtrlCheck(ConverterNativeMethods.CtrlTypes ctrlType)
        {
            Logger.Write(LogSource.Common, TraceLevel.Error, "Console Event Handler");

            switch (ctrlType)
            {
                case ConverterNativeMethods.CtrlTypes.CTRL_BREAK_EVENT:
                    return false;

                case ConverterNativeMethods.CtrlTypes.CTRL_C_EVENT:
                    return false;

                case ConverterNativeMethods.CtrlTypes.CTRL_CLOSE_EVENT:
                    Logger.Write(LogSource.Common, TraceLevel.Error, "Console Window closed");
                    CleanUpOnConsoleEvent();
                    break;

                case ConverterNativeMethods.CtrlTypes.CTRL_LOGOFF_EVENT:
                    Logger.Write(LogSource.Common, TraceLevel.Error, "Session log off");
                    CleanUpOnConsoleEvent();
                    break;

                case ConverterNativeMethods.CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    Logger.Write(LogSource.Common, TraceLevel.Error, "Machine shutdown");
                    CleanUpOnConsoleEvent();
                    break;
            }

            return true;
        }

        private static void CleanUpOnConsoleEvent()
        {
            string status = CurConResource.ControlKeyPressed;
            CleanUpUnexpectedAbort(status);
            Environment.Exit(-1);
        }

        /// <summary>
        /// Validate Configuration File parameter
        /// </summary>
        /// <param name="value">config xml file name</param>
        /// <returns>true if successful</returns>
        private static bool HandleConfig(string value)
        {
            // ensure the file validity
            UtilityMethods.ValidateFile(value);
            convParams.ConfigFile = value;
            return true;
        }

        /// <summary>
        /// Handle /s: argument
        /// </summary>
        /// <param name="value">Schema Map name</param>
        /// <returns></returns>
        private static bool HandleSchemaMap(string value)
        {
            convParams.SchemaMapFile = value;
            return true;
        }

        /// <summary>
        /// Display usage
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool DisplayHelp(string value)
        {
            Console.WriteLine();
            string assemblyName = CurrentAssembly.GetName().Name;
            
            // Fix for 57217 CQConverter - Help is not in standard format.	
            Console.WriteLine(UtilityMethods.Format(CurConResource.CurConHelpMessage, assemblyName));
            return false;
        }

        /// <summary>
        /// Validate inter arguments dependency
        /// </summary>
        /// <returns></returns>
        private static bool ValidateArgumentsDependency()
        {
            bool retval = true;
            
            if (convParams.SchemaMapFile != null)
            {
                Console.WriteLine(UtilityMethods.Format(CurConResource.SchemaMapNotRequired, convParams.SchemaMapFile));
                retval = false;
            }

            return retval;
        }

        /// <summary>
        /// Handler for Control-C for the Converter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            CleanUpUnexpectedAbort(CurConResource.ControlKeyPressed);

            // Allow the process to exit!
            e.Cancel = false;
        }

        /// <summary>
        /// Perform clean shutdown when either unhandled exception occurs
        /// OR Ctrl-C is triggered
        /// </summary>
        private static void CleanUpUnexpectedAbort(string errMsg)
        {
            lock (m_EndConverterLock)
            {
                if (m_UnexpectedTermination)
                {
                    return;
                }

                m_UnexpectedTermination = true;
            }

            // add a defensive check considering Ctrl-C is pressed even before the Reporting is init..
            if (MigrationReport != null)
            {
                MigrationReport.WriteIssue("ProcessStopped", ReportIssueType.Critical, errMsg);
            }

            Display.DisplayError(errMsg);
            ThreadManager.AbortThreads();
            EndConverter();
        }

        /// <summary>
        /// Returns the value for a given element from XML document
        /// </summary>
        /// <param name="doc">XML Document</param>
        /// <param name="element">Node Name</param>
        /// <returns>Node value, null id node does not exist</returns>
        private static string GetValueForNode(XmlDocument doc, string element)
        {
            XmlNodeList nodes = doc.GetElementsByTagName(element);
            if (nodes != null && nodes[0] != null)
            {
                return nodes[0].InnerText;
            }
            return null;
        }
    } // class ConverterMain
}
