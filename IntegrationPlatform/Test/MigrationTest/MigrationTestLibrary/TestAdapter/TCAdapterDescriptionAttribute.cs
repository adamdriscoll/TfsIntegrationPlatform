// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationTestLibrary
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TCAdapterDescriptionAttribute : Attribute
    {
        #region Fields
        private readonly Guid m_id;
        private readonly string m_name;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the TCAdapterDescriptionAttribute class.
        /// </summary>
        /// <param name="id">A static identifier for the Adapter. This must be a valid Guid string.</param>
        /// <param name="name">A friendly name for the Adapter.</param>
        public TCAdapterDescriptionAttribute(string id, string name)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            m_id = new Guid(id);
            m_name = name;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the Id of the Plugin.
        /// </summary>
        public Guid Id
        {
            get
            {
                return m_id;
            }
        }

        /// <summary>
        /// Gets the name of the Plugin.
        /// </summary>
        public string Name
        {
            get
            {
                return m_name;
            }
        }

        #endregion
    }
}
