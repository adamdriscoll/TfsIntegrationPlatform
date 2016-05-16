// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.TeamFoundation.Migration.Shell.Properties;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    #region Delegates
    /// <summary>
    /// Provides a parameterless, void delegate that can be used for simple anonymous methods.
    /// </summary>
    public delegate void AnonymousMethod ();
    #endregion

    /// <summary>
    /// Provides various utility methods that simplify certain EditorFoundation usage scenarios.
    /// </summary>
    public static class Utilities
    {
        private const string exceptionWriterName = "exceptionWriter";
        private static readonly TraceSource traceSource;
        private static string s_logFilePath;

        static Utilities ()
        {
            Utilities.traceSource = new TraceSource(Properties.Settings.Default.TraceSourceName, SourceLevels.All);
        }

        /// <summary>
        /// Gets the default trace source.
        /// </summary>
        /// <remarks>
        /// Set the TraceSourceName setting in the application configuration file to change the trace source name.
        /// The default trace source name is "Application".
        /// </remarks>
        /// <value>The default trace source.</value>
        public static TraceSource DefaultTraceSource
        {
            get
            {
                if (Utilities.traceSource.Listeners[exceptionWriterName] == null)
                {
                    string currProcName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                    string logFile = string.Format("_{0}_{1}_{2}.log",
                                                   currProcName ?? ShellResources.UnknownProcess,
                                                   ShellResources.Exception,
                                                   DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'_'mm'_'ss"));
                    string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Team Foundation\TFS Integration Platform\");

                    if (!Directory.Exists(logPath))
                    {
                        Directory.CreateDirectory(logPath);
                    }

                    s_logFilePath = Path.Combine(logPath, logFile);

                    StreamWriter myFile = File.CreateText(s_logFilePath);
                    myFile.AutoFlush = true;
                    Utilities.traceSource.Listeners.Add(new TextWriterTraceListener(myFile, exceptionWriterName));

                    Utilities.traceSource.TraceInformation(Assembly.GetExecutingAssembly().FullName);
                }
                return Utilities.traceSource;
            }
        }

        public static void ShowError(string message, string caption)
        {
            System.Windows.Window mainWindow = null;
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    mainWindow = System.Windows.Application.Current.MainWindow;
                    MessageBox.Show(new WindowWrapper(mainWindow), message,
                        caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
            catch (Exception)
            {
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void HandleException(Exception exception)
        {
            if (exception.InnerException == null)
            {
                Utilities.HandleException(exception, false, ShellResources.ErrorDialogTitle, "Error");
            }
            else
            {
                Utilities.HandleException(exception.InnerException, false, ShellResources.ErrorDialogTitle, exception.Message);
            }
        }

        /// <summary>
        /// Provides a default exception handler.
        /// </summary>
        /// <param name="exception">The exception being handled.</param>
        /// <param name="isSevere">If set to <c>true</c> the specified exception is considered a severe error.</param>
        /// <param name="messageCaption">A brief description of the error context.</param>
        /// <param name="messageHeader">A header for the error details.</param>
        /// <param name="messageHeaderFormatArgs">An <see cref="Object"/> array containing zero or more objects to format.</param>
        public static void HandleException (Exception exception, bool isSevere, string messageCaption, string messageHeader, params object[] messageHeaderFormatArgs)
        {
            if (exception != null)
            {
                // Format the message header
                if (messageHeaderFormatArgs != null && messageHeaderFormatArgs.Length > 0)
                {
                    messageHeader = string.Format(messageHeader, messageHeaderFormatArgs);
                }

                // Trace out the details of the error
                string traceMessage = exception.ToString();
                TraceEventType traceEventType = isSevere ? TraceEventType.Critical : TraceEventType.Error;
                Utilities.DefaultTraceSource.TraceEvent(traceEventType, 0, String.Format("[{0}] {1} ", DateTime.Now, traceMessage));

                // Display an error message to the user
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(messageHeader);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(string.Format("See {0}", s_logFilePath));
                string userMessage = stringBuilder.ToString();
                MessageBoxIcon messageBoxIcon = isSevere ? MessageBoxIcon.Stop : MessageBoxIcon.Error;
                System.Windows.Window mainWindow = null;
                try
                {
                    Dispatcher.CurrentDispatcher.Invoke((System.Action)(() => mainWindow = System.Windows.Application.Current.MainWindow));
                    MessageBox.Show(new WindowWrapper(mainWindow),
                        userMessage, messageCaption, MessageBoxButtons.OK, messageBoxIcon);
                }
                catch (Exception)
                {
                    MessageBox.Show(userMessage, messageCaption, MessageBoxButtons.OK, messageBoxIcon);
                }
            }
        }

        /// <summary>
        /// Gets an embedded image resource from the currently calling assembly.
        /// </summary>
        /// <param name="resourceName">The name of the resource.</param>
        /// <returns>The image.</returns>
        public static Image GetEmbeddedImage (string resourceName)
        {
            using (Stream imageStream = Assembly.GetCallingAssembly ().GetManifestResourceStream (resourceName))
            {
                return new Bitmap (Image.FromStream (imageStream));
            }
        }

        /// <summary>
        /// Gets the property descriptor for the specified object and property name.
        /// </summary>
        /// <remarks>
        /// If the specified property does not exist in the specified object,
        /// an ArgumentException is thrown.
        /// </remarks>
        /// <param name="obj">The object to which the specified property belongs.</param>
        /// <param name="propertyName">The property for which to obtain a property descriptor.</param>
        /// <returns>The property descriptor.</returns>
        public static PropertyDescriptor GetPropertyDescriptor (object obj, string propertyName)
        {
            PropertyDescriptor propertyDescriptor = Utilities.TryGetPropertyDescriptor (obj, propertyName);
            if (propertyDescriptor == null)
            {
                throw new ArgumentException (string.Format ("{0} is not a property of {1}", propertyName, obj.GetType ().Name));
            }
            return propertyDescriptor;
        }

        /// <summary>
        /// Gets the property descriptor for the specified object and property name.
        /// </summary>
        /// <remarks>
        /// If the specified property does not exist in the specified object,
        /// a null reference is returned.
        /// </remarks>
        /// <param name="obj">The object to which the specified property belongs.</param>
        /// <param name="propertyName">The property for which to obtain a property descriptor.</param>
        /// <returns>The property descriptor.</returns>
        public static PropertyDescriptor TryGetPropertyDescriptor (object obj, string propertyName)
        {
            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties (obj))
            {
                if (propertyDescriptor.Name == propertyName)
                {
                    return propertyDescriptor;
                }
            }

            return null;
        }

        /// <summary>
        /// Verifies that the specified data can be successfully serialized using the <c>BinaryFormatter</c>.
        /// </summary>
        /// <remarks>
        /// The specified data is serialized and then deserialized using the standard .NET <c>BinaryFormatter</c>.
        /// The deserialized data is returned, and can be inspected for integrity. If either the serialization
        /// or deserialization fails, an exception will be thrown.
        /// </remarks>
        /// <typeparam name="T">The type of the data to be serialized.</typeparam>
        /// <param name="data">The data to be serialized.</param>
        /// <returns>The deserialized data.</returns>
        public static T VerifySerializable<T> (T data)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter ();
            MemoryStream memoryStream = new MemoryStream ();
            binaryFormatter.Serialize (memoryStream, data);
            byte[] rawData = memoryStream.ToArray ();
            memoryStream.Close ();

            memoryStream = new MemoryStream (rawData);
            return (T)binaryFormatter.Deserialize (memoryStream);
        }

        //internal static void CopyTo<T> (this IEnumerable<T> collection, T[] array, int arrayIndex)
        //{
        //    int index = arrayIndex;
        //    foreach (T item in collection)
        //    {
        //        array[arrayIndex] = item;
        //    }
        //}

        public static bool IsGuid(string guid)
        {
            try
            {
                Guid g = new Guid(guid);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
