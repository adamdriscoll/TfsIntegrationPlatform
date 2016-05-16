// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.IO;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// Defines methods for serializing and deserializing a Model.
    /// </summary>
    public interface IModelSerializer
    {
        /// <summary>
        /// Serializes a Model.
        /// </summary>
        /// <param name="stream">The stream to which to serialize the Model.</param>
        /// <param name="model">The Model to serialize.</param>
        void Serialize(Stream stream, ModelObject model);

        /// <summary>
        /// Deserializes a Model.
        /// </summary>
        /// <param name="stream">The stream from which to deserialize the Model.</param>
        /// <returns>The deserialized Model.</returns>
        ModelObject Deserialize (Stream stream);
    }

}
