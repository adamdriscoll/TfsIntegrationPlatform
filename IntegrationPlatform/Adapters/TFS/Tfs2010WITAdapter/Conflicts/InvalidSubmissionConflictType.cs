// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.TeamFoundation.Migration.Toolkit;
using Microsoft.TeamFoundation.Migration.TfsWitAdapter.Common;

namespace Microsoft.TeamFoundation.Migration.Tfs2010WitAdapter.Conflicts
{
    /// <summary>
    /// The TFS WIT invalid revision submission conflict type.
    /// </summary>
    public class InvalidSubmissionConflictType : ConflictType
    {
        public static MigrationConflict CreateConflict(
            IMigrationAction action,
            Exception exception,
            string sourceItemId,
            string sourceItemRevision)
        {
            MigrationConflict conflict = new MigrationConflict(
                new InvalidSubmissionConflictType(),
                MigrationConflict.Status.Unresolved,
                CreateConflictDetails(action, exception, sourceItemId, sourceItemRevision),
                CreateScopeHint(exception, sourceItemId, sourceItemRevision));
            conflict.ConflictedChangeAction = action;

            return conflict;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public InvalidSubmissionConflictType()
            : base(new InvalidSubmissionConflictHandler())
        {
        }

        /// <summary>
        /// Gets the reference name of this conflict type.
        /// </summary>
        public override Guid ReferenceName
        {
            get
            {
                return InvalidSubmissionConflictTypeConstants.ReferenceName;
            }
        }

        /// <summary>
        /// Gets the friendly name of this conflict type.
        /// </summary>
        public override string FriendlyName
        {
            get
            {
                return InvalidSubmissionConflictTypeConstants.FriendlyName;
            }
        }

        public override string TranslateConflictDetailsToReadableDescription(string dtls)
        {
            if (string.IsNullOrEmpty(dtls))
            {
                throw new ArgumentNullException("dtls");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(InvalidSubmissionConflictDetails));

            using (StringReader strReader = new StringReader(dtls))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                InvalidSubmissionConflictDetails details =
                    (InvalidSubmissionConflictDetails)serializer.Deserialize(xmlReader);

                return string.Format(
                    "Source Item {0} (revision {1}) is invalid. The submission to TFS server threw the following exception:\n  {2}.",
                    details.SourceItemId,
                    details.SourceItemRevision,
                    string.Format("  Exception: {0}\n  Message: {1}\n  Inner Exception: {2}\n  Inner Exception Message: {3}",
                        details.ExceptionType, details.ExceptionMessage, details.InnerExceptionType, details.InnerExceptionMessage));
            }
        }

        protected override void RegisterDefaultSupportedResolutionActions()
        {
            foreach (var action in InvalidSubmissionConflictTypeConstants.SupportedActions)
            {
                AddSupportedResolutionAction(action);
            }
        }

        internal static InvalidSubmissionConflictDetails GetConflictDetails(MigrationConflict conflict)
        {
            if (!conflict.ConflictType.ReferenceName.Equals(InvalidSubmissionConflictTypeConstants.ReferenceName))
            {
                throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(conflict.ConflictDetails))
            {
                throw new ArgumentNullException("conflict.ConflictDetails");
            }

            XmlSerializer serializer = new XmlSerializer(typeof(InvalidSubmissionConflictDetails));

            using (StringReader strReader = new StringReader(conflict.ConflictDetails))
            using (XmlReader xmlReader = XmlReader.Create(strReader))
            {
                return serializer.Deserialize(xmlReader) as InvalidSubmissionConflictDetails;
            }
        }

        private static string CreateScopeHint(
            Exception ex,
            string sourceItemId,
            string sourceItemRevision)
        {
            if (ex.InnerException != null)
            {
                // format: /<exception type>/<exception message>/<inner exception type>/<inner exception message>/SourceItemId/SourceItemRevision
                return string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}",
                    BasicPathScopeInterpreter.PathSeparator, ex.GetType().ToString(),
                    BasicPathScopeInterpreter.PathSeparator, ex.Message ?? string.Empty,
                    BasicPathScopeInterpreter.PathSeparator, ex.InnerException.GetType().ToString(),
                    BasicPathScopeInterpreter.PathSeparator, ex.InnerException.Message ?? string.Empty,
                    BasicPathScopeInterpreter.PathSeparator, sourceItemId,
                    BasicPathScopeInterpreter.PathSeparator, sourceItemRevision);
            }
            else
            {
                // format: /<exception type>/<exception message>/SourceItemId/SourceItemRevision
                return string.Format("{0}{1}{2}{3}{4}{5}{6}{7}",
                    BasicPathScopeInterpreter.PathSeparator, ex.GetType().ToString(),
                    BasicPathScopeInterpreter.PathSeparator, ex.Message ?? string.Empty,
                    BasicPathScopeInterpreter.PathSeparator, sourceItemId,
                    BasicPathScopeInterpreter.PathSeparator, sourceItemRevision);
            }
        }

        private static string CreateConflictDetails(
            IMigrationAction action,
            Exception exception,
            string sourceItemId,
            string sourceItemRevision)
        {
            InvalidSubmissionConflictDetails dtls = new InvalidSubmissionConflictDetails(
                new XmlDocument(), exception, sourceItemId, sourceItemRevision);

            XmlSerializer serializer = new XmlSerializer(typeof(InvalidSubmissionConflictDetails));
            using (MemoryStream memStrm = new MemoryStream())
            {
                serializer.Serialize(memStrm, dtls);
                memStrm.Seek(0, SeekOrigin.Begin);
                using (StreamReader sw = new StreamReader(memStrm))
                {
                    return sw.ReadToEnd();
                }
            }
        }
    }

    [Serializable]
    public class InvalidSubmissionConflictDetails
    {
        public InvalidSubmissionConflictDetails()
        { }

        public InvalidSubmissionConflictDetails(
            XmlDocument actionData,
            Exception exception,
            string sourceItemId,
            string sourceItemRevision)
        {
            Debug.Assert(actionData != null, "actionData is Null");
            ActionData = actionData.OuterXml;
            SourceItemId = sourceItemId;
            SourceItemRevision = sourceItemRevision;

            ExceptionType = exception == null ? string.Empty : exception.GetType().ToString();
            ExceptionMessage = exception == null ? string.Empty : (exception.Message ?? string.Empty);
            InnerExceptionType = exception.InnerException == null ? string.Empty : exception.InnerException.GetType().ToString();
            InnerExceptionMessage = exception.InnerException == null ? string.Empty : (exception.InnerException.Message ?? string.Empty);
        }

        public string ExceptionType
        {
            get;
            set;
        }

        public string ExceptionMessage
        {
            get;
            set;
        }

        public string InnerExceptionType
        {
            get;
            set;
        }

        public string InnerExceptionMessage
        {
            get;
            set;
        }

        public string ActionData
        {
            get;
            set;
        }

        public string SourceItemId
        {
            get;
            set;
        }

        public string SourceItemRevision
        {
            get;
            set;
        }
    }
}
