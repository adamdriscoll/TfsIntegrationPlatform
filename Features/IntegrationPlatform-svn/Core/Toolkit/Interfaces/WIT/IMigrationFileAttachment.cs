// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    /// <summary>
    /// Represents a file attached to a work item.
    /// </summary>
    public interface IMigrationFileAttachment
    {
        /// <summary>
        /// Returns the name of the file.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the file length/size.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Returns the timestamp when the file was created as a UTC datetime.
        /// </summary>
        DateTime UtcCreationDate { get; }

        /// <summary>
        /// Returns the timestamp when the file was last updated as a UTC datetime.
        /// </summary>
        DateTime UtcLastWriteDate { get; }

        /// <summary>
        /// Returns a comment about the file.
        /// </summary>
        string Comment { get; }

        /// <summary>
        /// Gets the contents of the file when needed for comparison.
        /// </summary>
        /// <returns>Contents of the file.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        Stream GetFileContents();
    }
}
