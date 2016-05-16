// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

// File with some utility functions useful for converters.

using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.TeamFoundation.Client;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Text;


namespace Microsoft.TeamFoundation.Converters.Utility
{
    internal static class UtilityMethods
    {
        #region Xml Validation methods
        /// <summary>
        /// This method will validate the given xml file against the givne xsd file and if the file is in correct format
        /// return true indicating the sucess otherwise false.
        /// </summary>
        /// <param name="xmlFile"></param>
        /// <param name="xsdFile"></param>
        /// <returns>XmlDocument object with xml file contents</returns>
        internal static XmlDocument ValidateXmlFile(string xmlFile, string xsdFile)
        {
            XmlReader reader = null;

            // validate if the file is valid and the process has read permissions
            ValidateFile(xmlFile);

            try
            {
                //Assuming that the xsdfile is embedded in the assembly the file is being opened.
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(xsdFile))
                {
                    XmlSchema schema = XmlSchema.Read(new XmlTextReader(stream), null);
                    // Load XML document with validator
                    XmlDocument xmldoc = new XmlDocument();
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.Schemas.Add(schema);
                    settings.ValidationType = ValidationType.Schema;
                    settings.ConformanceLevel = ConformanceLevel.Auto;

                    using (reader = XmlReader.Create(new XmlTextReader(xmlFile), settings))
                    {
                        xmldoc.Load(reader);
                    }
                    return xmldoc;
                }

            }
            catch (XmlSchemaValidationException e)
            {
                string errMsg = UtilityMethods.Format(
                    CommonResource.XmlSchemaValidationFailed,
                    xmlFile, e.LineNumber, e.LinePosition, e.Message);
                Logger.Write(LogSource.Common, TraceLevel.Error, "Validation of XML Schema for '{0}' Failed", xmlFile);
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(errMsg, e);
            }
            catch (XmlException e)
            {
                string errMsg = UtilityMethods.Format(
                    CommonResource.XmlFileValidationFailed, xmlFile, e.Message);
                Logger.Write(LogSource.Common, TraceLevel.Error, "XML file is in wrong format: {0}", xmlFile);
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(errMsg, e);
            }
        }

        /// <summary>
        /// This method will validate the given xml fragment against the given xsd file 
        /// </summary>
        /// <param name="node">XML file name from which the fragment has to be validated</param>
        /// <param name="node">Node with the XML fragment</param>
        /// <param name="xsdFile">XSD file name to validate</param>
        /// <returns>XmlDocument object with xml file contents</returns>
        [SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]
        internal static XmlDocument ValidateXmlFragment(string xmlFileName, XmlNode node, string xsdFile)
        {
            XmlParserContext context = new XmlParserContext(null, null, string.Empty, XmlSpace.None);
            XmlReader reader = null;
            try
            {
                //Assuming that the xsd file is embedded in the assembly the file is being opened.
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(xsdFile))
                {
                    XmlSchema schema = XmlSchema.Read(new XmlTextReader(stream), null);
                    // Load XML document with validator
                    XmlDocument xmldoc = new XmlDocument();
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.Schemas.Add(schema);
                    settings.ValidationType = ValidationType.Schema;
                    settings.ConformanceLevel = ConformanceLevel.Auto;

                    using (reader = XmlReader.Create(new XmlTextReader(node.OuterXml, XmlNodeType.Element, context), settings))
                    {
                        xmldoc.Load(reader);
                    }
                    return xmldoc;
                }

            }
            catch (XmlSchemaValidationException e)
            {
                string errMsg = UtilityMethods.Format(
                    CommonResource.XmlSchemaValidationFailed,
                    xmlFileName, e.LineNumber, e.LinePosition, e.Message);
                Logger.Write(LogSource.Common, TraceLevel.Error, "Validation of XML Schema for '{0}' Failed", xsdFile);
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(errMsg, e);
            }
            catch (Exception e) //for any other exception just throw ConverterException to communicate failure
            {
                string errMsg = UtilityMethods.Format(
                    CommonResource.XmlFileValidationFailed,
                    xmlFileName, e.Message);
                Logger.Write(LogSource.Common, TraceLevel.Error, "Validation of XML Schema for '{0}' Failed", xsdFile);
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(errMsg, e);
            }
        }

        /// <summary>
        /// Loads file with the specified name using StreamReader
        /// The caller is suppose to do the xsd and existence validation
        /// and also to catch any exceptions if occurs
        /// </summary>
        /// <param name="fileName">File Name</param>
        /// <returns>File content loaded as XmlDocument</returns>
        [SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]
        internal static XmlDocument LoadFileAsXmlDocument(string fileName)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                return doc;
            }
        }

        #endregion

        /// <summary>
        /// This is a method which creates the specified directory
        /// </summary>
        /// <param name="name"></param>
        /// The path should not contain invalid characters such as ", <, >, or |.
        /// Not catching pathtoolongexception
        internal static void CreateDirectory(string name)
        {
            Debug.Assert(name != null, "Directory name specified is null");
            Debug.Assert(name.Length != 0, "Directory name specified is empty");

            try
            {
                DirectoryInfo dir = new DirectoryInfo(name);
                if (!dir.Exists)
                {
                    dir.Create();
                }
            }
            // any exception just wrap it and rethrow to communicate failure
            catch (Exception e)
            { 
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(e.Message, e);
            }
        }

        internal static void MonitorWait(object waiter)
        {
            MonitorWait(waiter, false);
        }

        internal static void MonitorWait(object waiter, bool exitContext)
        {
            while (Monitor.Wait(waiter, 1000, exitContext))
            {
                if (ThreadManager.IsAborting)
                {
                    throw new ConverterAbortingException();
                }
            }
        }


        /// <summary>
        /// This is a method to copy the resource file in the assembly to the specified file.
        /// /// Not catching Path too long exception
        /// </summary>
        /// <param name="sourceName"></param>
        /// <param name="destinationName"></param>
        internal static void CopyFromAssemblyToDestination(string sourceName, string destinationName)
        {
            Debug.Assert(sourceName != null, "Source file name in the assembly is null");
            Debug.Assert(destinationName != null, "Destination file name is null");

            Debug.Assert(sourceName.Length != 0, "Source file name in the assembly is empty");
            Debug.Assert(destinationName.Length != 0, "Destination file name is empty");

            try
            {
                using (Stream stream = GetManifestResourceStream(sourceName))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        using (BinaryWriter writer = new BinaryWriter(new FileStream(destinationName, FileMode.Create)))
                        {
                            int cnt = 0;
                            while (cnt < stream.Length)
                            {
                                writer.Write(reader.ReadByte());
                                cnt++;
                            }
                        }
                    }
                }
            }
            // Not catching UriFormatException, DirectoryNotFoundException, NotSupportedException, FileNotFoundException
            // as any of these exception indicates a programming error and the end user input is never passed to this method.
            catch (UnauthorizedAccessException e) // this is thrown if the file exists with that name and is readonly.
            {
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(e.Message, e);
            }
        }

        private static Stream GetManifestResourceStream(string sourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = GetManifestResourceStreamForCulture(assembly, sourceName, CultureInfo.CurrentUICulture);
            // not present in current ui culture get neutral culture
            if (stream == null)
            {
                stream = GetManifestResourceStreamForCulture(assembly, sourceName, CultureInfo.CurrentUICulture.Parent);
            }
            // Last fall back option get from assembly (ENU)
            // 0 length stream is for image files these should be taken from exe itself
            if (stream == null || stream.Length == 0) 
            {
                stream = assembly.GetManifestResourceStream(sourceName);
            }

            return stream;
        }

        internal static Stream GetManifestResourceStreamForCulture(Assembly assembly, string sourceName, CultureInfo culture)
        {
            Stream stream = null;
            try
            {
                stream = assembly.GetSatelliteAssembly(culture).GetManifestResourceStream(sourceName);
            }
            catch (FileNotFoundException)// not found for specified culture
            {
            }
            return stream;
        }

        public static TeamFoundationServer ValidateAndGetTFS(string tfsName)
        {
            try
            {
                TeamFoundationServer tfs = TeamFoundationServerFactory.GetServer(tfsName);
                tfs.EnsureAuthenticated();
                return tfs;
            }
            catch (Exception e)
            {
                string errString = null;
                if (e is System.Security.SecurityException ||
                    e is TeamFoundationServerUnauthorizedException)
                {
                    errString = CommonResource.TfsNotAuthorized;
                }
                else if (e is System.Net.WebException)
                {
                    errString = CommonResource.TfsConnectFailure;
                }
                else if (e is InvalidOperationException) // e.g the server returned text response when xml was expected
                {
                    errString = CommonResource.TfsConnectFailure;
                }

                if (errString == null) // Unknown error
                    throw;

                string errMsg = UtilityMethods.Format(errString, tfsName);
                Logger.Write(LogSource.Common, TraceLevel.Error, "Could not connect to Team Foundation Server {0}", tfsName);
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(errMsg);
            }
        }

        internal static string GetUniquePath(string parentDirectoryName)
        {
            string tempRootName;
            do
            {
                tempRootName = Path.Combine(parentDirectoryName, Path.GetRandomFileName());
            } while (File.Exists(tempRootName) && Directory.Exists(tempRootName));
            return tempRootName;
        }


        #region File open methods

        /// <summary>
        /// This validates if the given path is correct. This method
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static void ValidateFile(string fileName)
        {
            ValidateFile(fileName, null);
        }

        internal static void ValidateFile(string fileName, string parentFilename)
        {
            Debug.Assert(fileName != null, "File name cannot be null");
            Debug.Assert(fileName.Length != 0, "File name cannot be empty");

            try
            {
                FileStream fs = File.OpenRead(fileName);
                fs.Close();
            }
            // could not validate file throw ConverterException
            catch (Exception e)
            {
                string errMsg;
                if (parentFilename == null)
                {
                    errMsg = UtilityMethods.Format(
                        CommonResource.FileAccessError, fileName, e.Message);
                }
                else
                {
                    errMsg = UtilityMethods.Format(
                        CommonResource.FileAccessSchemaError, fileName, parentFilename, e.Message);
                }
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(errMsg, e);
            }
        }

        /// <summary>
        /// Returns true if file is writable, otherwise false
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>true if writable</returns>
        [SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode", Justification="This is used by SDConverter")]
        internal static bool IsWritable(string fileName)
        {
            FileAttributes attributes = File.GetAttributes(fileName);
            if ((attributes & (FileAttributes.ReadOnly | FileAttributes.Directory)) != 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// This method can be used to check if a file with 
        /// given name can be created in the given path.
        /// Basically checking the validity of the path.
        ///</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static void AbleToCreateFile(string fileName)
        {
            Debug.Assert(fileName != null, "File name cannot be null");
            Debug.Assert(fileName.Length != 0, "File name cannot be empty");
            try
            {
                string directoryName = Path.GetDirectoryName(fileName);

                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                using (FileStream stream = File.Create(fileName))
                {
                    // Just try and create the file. 
                    // If the file cannot be created then there is something 
                    // wrong in the input path/filename.
                }
                File.Delete(fileName);
            }
            // for any error just wrap it and throw so that application 
            // can exit garcefully.
            catch (Exception e)
            {
                Logger.WriteException(LogSource.Common, e);
                throw new ConverterException(e.Message, e);
            }            
        }
        #endregion


        #region Utility Display message
        /// <summary>
        /// write the error message to the standard error
        /// </summary>
        /// <param name="msg"></param>
        internal static void DisplayError(string msg)
        {
            //print the error message to console.error.
            Console.Error.WriteLine(msg);
        }

        /// <summary>
        /// write a message to the standard output
        /// </summary>
        /// <param name="msg"></param>
        internal static void DisplayOutput(string msg)
        {
            //print the message to console.output
            Console.WriteLine(msg);
        }
        #endregion

        /// <summary>
        /// Utility method to do string.Format(CultureInfo.InvariantCulture) call
        /// </summary>
        /// <param name="format">The format string</param>
        /// <param name="parameters">The parameters</param>
        /// <returns>Formatted string</returns>
        public static string Format(string format, params object[] parameters)
        {
            Debug.Assert(!string.IsNullOrEmpty(format), "format is null");
            Debug.Assert(parameters != null, "parameters is null - no need to call Format()");

            return string.Format(CultureInfo.InvariantCulture, format, parameters);
        }

        /// <summary>
        /// This method returns full name of the file. If it cannot it returns same
        /// filename errors.
        /// </summary>
        /// <param name="argumentValue">file name</param>
        /// <returns>Full name of the file</returns>
        internal static string GetFullName(string fileName)
        {
            try
            {
                fileName = Path.GetFullPath(fileName);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception)
            {
                // do nothing
            }

            return fileName;
        }

        internal static bool RetryDiskFull(long spaceRequired, string errorMessage)
        {
            bool IsUp = false;

            // Retry logic for Disk Full
            string systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            Display.RaiseBlockingError(errorMessage);
            DriveInfo driveInfo = new DriveInfo(systemDrive);
            try
            {
                Logger.Write(LogSource.Common, TraceLevel.Error, "Disk Full. Retrying every {0} seconds...", DiskFullWaitTime);
                while (!IsUp)
                {
                    if (ThreadManager.IsAborting)
                    {
                        Thread.CurrentThread.Abort();
                        break;
                    }

                    //atleast 5 mb of space
                    if (driveInfo.AvailableFreeSpace > spaceRequired)
                    {
                        Logger.Write(LogSource.Common, TraceLevel.Error, "at least 5 mb of space is there.");
                        IsUp = true;
                        break;
                    }

                    for (int i = 0; i < DiskFullWaitTime; i++)
                    {
                        if (ThreadManager.IsAborting)
                        {
                            Thread.CurrentThread.Abort();
                            IsUp = true;
                            break;
                        }

                        Thread.Sleep(1000);
                    }
                }
            }
            finally
            {
                Display.SetBlockingErrorResolved();
            }
            return IsUp;
        }

        internal static bool RetryDiskFull()
        {
            string systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            string errorMessage =
                UtilityMethods.Format(CommonResource.DiskFull, systemDrive);
            long requiredSpace = 5 * 1024 * 1024; // default to 5 mb
            return RetryDiskFull(requiredSpace, errorMessage);
        }

        internal static bool RetryDiskFull(long requiredSpace)
        {
            string systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            string errorMessage =
                UtilityMethods.Format(CommonResource.DiskFull, systemDrive);
            return RetryDiskFull(requiredSpace, errorMessage);
        }

        internal static void RetryDiskFull(string fileName)
        {
            string errorMessage =
                UtilityMethods.Format(CommonResource.ErrorWritingFileRetry, fileName);
            bool retry = true;
            DisplayOutput(errorMessage);

            while (!retry)
            {
                try
                {
                    Console.ReadKey(true);
                }
                catch (InvalidOperationException ioe)
                {
                    // if console stdin is redirected (e.g. from a text file of responses) ReadKey will throw
                    // an InvalidOperationException - ReadLine does work in this situation so fallback to that.
                    Logger.WriteExceptionAsWarning(LogSource.Common, ioe);
                    Console.ReadLine();
                }

                try
                {
                    AbleToCreateFile(fileName);
                    retry = false;
                }
                catch (ConverterException e)
                {
                    DisplayError(
                        UtilityMethods.Format(CommonResource.FileWriteError, 
                        fileName, e.Message));

                    DisplayOutput(errorMessage);
                }
            }
        }


        /// <summary>
        /// Verifies whether the IOException corresponds to "There is not enough space on the disk" error. 
        /// Using the HResult value (-2147024784 - ERROR_DISK_FULL)to determine "Disk out of space"
        /// </summary>
        /// <param name="ioException"></param>
        /// <returns>True if the exception corresponds to "There is not enough space on the disk" error
        /// else false</returns>
        internal static bool IsDiskOutOfSpaceError(IOException ioException)
        {
            // HResult is a protected memeber defined in the base Exception class. Using reflection
            // to retrieve this value
            Type exceptionType = ioException.GetType();
            PropertyInfo hResultProperty = exceptionType.GetProperty("HResult", BindingFlags.Instance | BindingFlags.NonPublic);
            if (hResultProperty != null)
            {
                int hResultValue = (int)hResultProperty.GetValue(ioException, null);
                if (-2147024784 == hResultValue)
                {
                    return true;
                }
            }
            return false;
        }
        private const int DiskFullWaitTime = 300; // time in seconds - 5 minutes
        public const int ERROR_DISK_FULL = 112;
    }

    internal static class ThreadManager
    {
        private static volatile bool m_aborting;
        private static Dictionary<int, Thread> m_threads = new Dictionary<int,Thread>();

        /// <summary>
        /// Creates a background thread
        /// </summary>
        internal static Thread CreateThread(ThreadStart threadStart, bool background)
        {
            Thread t = null;
            
            lock (m_threads)
            {
                demandNotAborting();

                t = new Thread(threadStart);
                t.IsBackground = background;
                AddThreadToManagerList(t);
            }

            return t;
        }

        /// <summary>
        /// Creates a background thread
        /// </summary>
        /// <param name="threadStart"></param>
        /// <returns></returns>
        internal static Thread CreateThread(ParameterizedThreadStart threadStart, bool background)
        {
            Thread t = null;

            lock (m_threads)
            {
                demandNotAborting();

                t = new Thread(threadStart);
                t.IsBackground = background;
                AddThreadToManagerList(t);
            }

            return t;
        }

        internal static void AddThreadToManagerList(Thread thread)
        {
            lock (m_threads)
            {
                demandNotAborting();

                if (!m_threads.ContainsKey(thread.ManagedThreadId))
                {
                    m_threads.Add(thread.ManagedThreadId, thread);
                }
            }
        }
        

        internal static void AbortThreads()
        {
            if (!m_aborting)
            {
                lock (m_threads)
                {
                    if (!m_aborting)
                    {
                        m_aborting = true;

                        foreach (Thread t in m_threads.Values)
                        {
                            if (t.IsAlive && (t.ManagedThreadId != Thread.CurrentThread.ManagedThreadId))
                            {
                                t.Abort();
                            }
                        }

                        foreach (Thread t in m_threads.Values)
                        {
                            if (t.IsAlive && (t.ManagedThreadId != Thread.CurrentThread.ManagedThreadId))
                            {
                                t.Join();
                            }
                        }

                        m_threads.Clear();
                    }
                }
            }
        }

        internal static bool IsAborting
        {
            get
            {
                return m_aborting;
            }
        }

        private static void demandNotAborting()
        {
            if(m_aborting)
            {
                throw new ConverterAbortingException(CommonResource.ProcessAborting);
            }
        }
    }
}
