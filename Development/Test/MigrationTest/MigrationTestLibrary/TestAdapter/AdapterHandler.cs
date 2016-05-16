// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationTestLibrary
{
    internal class AdapterHandler
    {
        private readonly Type m_type;
        private readonly TCAdapterDescriptionAttribute m_attributes;

        public Guid AdapterId
        {
            get
            {
                return m_attributes.Id;
            }
        }

        public string AdapterName
        {
            get
            {
                return m_attributes.Name;
            }
        }

        public Type AdapterType
        {
            get
            {
                return m_type;
            }
        }

        private AdapterHandler(Type type, TCAdapterDescriptionAttribute adapterDescriptionAttribute)
        {
            m_attributes = adapterDescriptionAttribute;
            m_type = type;
        }

        public static AdapterHandler FromType(Type type)
        {
            object[] attributes = type.GetCustomAttributes(typeof(TCAdapterDescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                return new AdapterHandler(type, (TCAdapterDescriptionAttribute)attributes[0]);
            }
            return null;
        }

        public ITestCaseAdapter CreateAdapter()
        {
            return (ITestCaseAdapter)Activator.CreateInstance(m_type);
        }
    }
}
