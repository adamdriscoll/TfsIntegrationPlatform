// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.TeamFoundation.Migration.Shell.Search;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    /// <summary>
    /// This class serves as a generic base class for the root object of a Model.
    /// </summary>
    [Serializable]
    public abstract class ModelRoot : ModelObject
    {
        #region Fields
        private string diskSyncHash;
        // Register your Serializer here, or instantiate XmlModelSerializer and stick it in.
        private static readonly Dictionary<Type, IModelSerializer> serializers;
        #endregion

        #region Constructors
        static ModelRoot ()
        {
            ModelRoot.serializers = new Dictionary<Type, IModelSerializer>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Indicates whether the Model has unsaved changes.
        /// </summary>
        [Browsable (false)]
        [Searchable (false)]
        public bool HasUnsavedChanges
        {
            get
            {
                return ModelRoot.GetHash (this) != this.diskSyncHash;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Saves the state of the Model to disk.
        /// </summary>
        /// <param name="path">The full path to which to save the Model.</param>
        public void Save (string path)
        {
            using (FileStream fileStream = new FileStream (path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                this.Save (fileStream, path);
            }
        }

        /// <summary>
        /// Saves the state of the Model to a stream.
        /// </summary>
        /// <param name="stream">The stream to which to save the Model.</param>
        public void Save (Stream stream)
        {
            this.Save (stream, null);
        }

        /// <summary>
        /// Generates a hash that uniquely identifies the state of the model.
        /// </summary>
        /// <param name="model">The model to hash.</param>
        /// <returns>A string that uniquely identifies the state of the model.</returns>
        /// <remarks>
        /// Model state hashing is useful for tasks such as change tracking. If the hash is different,
        /// then the model states are different. If the hash is the same, then the model states are the same.
        /// </remarks>
        public static string GetHash (ModelRoot model)
        {
            using (MemoryStream memoryStream = new MemoryStream ())
            {
                return model.Serialize (memoryStream);
            }
        }
        #endregion

        #region Internal Methods
        internal bool HashEquals (ModelRoot model)
        {
            return ModelRoot.GetHash (this).Equals (ModelRoot.GetHash (model));
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Loads a Model from a file, generating a hash in the process.
        /// The hash is used to determine whether the Model has unsaved changes.
        /// </summary>
        /// <typeparam name="T">The type of the root object in the Model.</typeparam>
        /// <param name="path">The full path from which to load the Model.</param>
        /// <returns>The deserialized Model.</returns>
        protected internal static T Load<T> (string path) where T : ModelRoot
        {
            using (FileStream fileStream = File.OpenRead (path))
            {
                T model = ModelRoot.Load<T> (fileStream, path);
                return model;
            }
        }

        /// <summary>
        /// Loads a model from a stream, generating a hash in the process.
        /// The hash is used to determine whether the Model has unsaved changes.
        /// </summary>
        /// <typeparam name="T">The type of the root object in the Model.</typeparam>
        /// <param name="stream">The stream from which to load the Model.</param>
        /// <returns>The deserialized Model.</returns>
        protected internal static T Load<T> (Stream stream) where T : ModelRoot
        {
            return ModelRoot.Load<T> (stream, null);
        }

        /// <summary>
        /// Creates a new instance of the Model and generates an associated hash.
        /// The hash is used to determine whether the Model has unsaved changes.
        /// </summary>
        /// <typeparam name="T">The type of the root object in the Model.</typeparam>
        /// <returns>A newly created Model.</returns>
        protected internal static T Create<T> () where T : ModelRoot, new ()
        {
            // Create a new model
            T model = new T ();

            // Get the contents and hash
            model.diskSyncHash = ModelRoot.GetHash (model);

            model.OnAfterCreate ();

            // Return the model
            return model;
        }

        /// <summary>
        /// Associates an IModelSerializer with a specific ModelRoot type.
        /// </summary>
        /// <typeparam name="T">The specific ModelRoot type.</typeparam>
        /// <param name="serializer">The serializer to use for serializing and deserializing the specific ModelRoot type.</param>
        protected static void RegisterSerializer<T>(IModelSerializer serializer) where T : ModelRoot
        {
            Type type = typeof(T);
            if (ModelRoot.serializers.ContainsKey(type))
            {
                throw new Exception(string.Format("A serializer for type {0} is already registered", type.Name));
            }
            ModelRoot.serializers[type] = serializer;
        }

        /// <summary>
        /// Called before the Model is saved.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnBeforeSave in a derived class, calling the base class's OnBeforeSave method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="path">The path to which the Model is being saved.</param>
        protected virtual void OnBeforeSave (string path)
        {
        }

        /// <summary>
        /// Called after the Model is saved.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnAfterSave in a derived class, calling the base class's OnAfterSave method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="path">The path to which the Model was saved.</param>
        protected virtual void OnAfterSave (string path)
        {
        }

        /// <summary>
        /// Called after the Model is loaded.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnAfterLoad in a derived class, calling the base class's OnAfterLoad method is not necessary because there is no initial implementation.
        /// </remarks>
        /// <param name="path">The path from which the Model was loaded.</param>
        protected virtual void OnAfterLoad (string path)
        {
        }

        /// <summary>
        /// Called after the Model is created.
        /// </summary>
        /// <remarks>
        /// Notes to Inheritors: When overriding OnAfterCreate in a derived class, calling the base class's OnAfterCreate method is not necessary because there is no initial implementation.
        /// </remarks>
        protected virtual void OnAfterCreate ()
        {
        }

        #endregion

        #region Private Methods
        private static T Load<T> (Stream stream, string path) where T : ModelRoot
        {
            SHA256 hashAlgorithm = SHA256.Create ();
            T model;
            using (CryptoStream cryptoStream = new CryptoStream (stream, hashAlgorithm, CryptoStreamMode.Read))
            {
                model = (T)GetSerializer(typeof(T)).Deserialize(cryptoStream);
            }
            // Set the disk sync hash and return the model
            model.diskSyncHash = Encoding.ASCII.GetString (hashAlgorithm.Hash);
            model.OnAfterLoad (path);
            return model;
        }

        private void Save (Stream stream, string path)
        {
            this.OnBeforeSave (path);
            this.diskSyncHash = this.Serialize (stream);
            this.OnAfterSave (path);
        }

        /// <summary>
        /// Serializes the Model to a stream, generating a hash in the process.
        /// The hash is used to determine whether the Model has unsaved changes.
        /// </summary>
        /// <param name="stream">The stream to which to serialize the Model.</param>
        /// <returns>The hash of the serialized Model.</returns>
        private string Serialize(Stream stream)
        {
            SHA256 hashAlgorithm = SHA256.Create();
            using (CryptoStream cryptoStream = new CryptoStream(stream, hashAlgorithm, CryptoStreamMode.Write))
            {
                GetSerializer(this.GetType()).Serialize(cryptoStream, this);

                cryptoStream.Close();
                return Encoding.ASCII.GetString(hashAlgorithm.Hash);
            }
        }

        private static IModelSerializer GetSerializer(Type type)
        {
            // custom serialization
            for (Type matchedType = type; matchedType != null; matchedType = matchedType.BaseType)
            {
                IModelSerializer serializer;
                if (ModelRoot.serializers.TryGetValue (matchedType, out serializer))
                {
                    return serializer;
                }
            }

            throw new Exception (String.Format ("No serializer registered for {0}", type.Name));
        }
        #endregion
    }
}
