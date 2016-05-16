// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Linq;
using Microsoft.TeamFoundation.Migration.EntityModel;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    public class ProviderHandler
    {
        #region Fields
        private Type m_type;
        private ProviderDescriptionAttribute m_descriptionAttribute;
        private ProviderCapabilityAttribute m_capabilityAttribute;
        private IProvider m_provider;
        private int m_internalId;
        #endregion

        #region Properties
        public string ProviderName
        {
            get
            {
                return m_descriptionAttribute.Name;
            }
        }

        public Guid ProviderId
        {
            get
            {
                return m_descriptionAttribute.Id;
            }
        }

        public string ProviderVersion
        {
            get
            {
                return m_descriptionAttribute.Version;
            }
        }

        public IProvider Provider
        {
            get
            {
                return m_provider;
            }
        }

        public ProviderDescriptionAttribute ProviderDescriptionAttribute
        {
            get
            {
                return m_descriptionAttribute;
            }
        }

        public ProviderCapabilityAttribute ProviderCapabilityAttribute
        {
            get
            {
                return m_capabilityAttribute;
            }
        }

        internal int InternalId
        {
            get
            {
                return m_internalId;
            }
        }

        internal bool VersionChanged
        {
            get;
            set;
        }

        #endregion

        #region Constructors
        internal ProviderHandler(ProviderHandler fromCopy)
        {
            m_type = fromCopy.m_type;
            m_descriptionAttribute = fromCopy.m_descriptionAttribute;
            m_capabilityAttribute = fromCopy.m_capabilityAttribute;
            m_provider = (IProvider)Activator.CreateInstance(m_type);
            m_internalId = fromCopy.InternalId;
            VersionChanged = fromCopy.VersionChanged;
        }

        private ProviderHandler(
            Type type, 
            ProviderDescriptionAttribute pluginDescriptionAttribute)
        {
            Initialize(type, pluginDescriptionAttribute, new ProviderCapabilityAttribute());
        }

        private ProviderHandler(
            Type type, 
            ProviderDescriptionAttribute pluginDescriptionAttribute,
            ProviderCapabilityAttribute capabilityAttributes)
        {
            Initialize(type, pluginDescriptionAttribute, capabilityAttributes);
        }

        private void Initialize(
            Type type,
            ProviderDescriptionAttribute pluginDescriptionAttribute,
            ProviderCapabilityAttribute capabilityAttributes)
        {
            this.m_type = type;
            m_descriptionAttribute = pluginDescriptionAttribute;
            m_capabilityAttribute = capabilityAttributes;
            m_provider = (IProvider)Activator.CreateInstance(type);
        }

        #endregion

        #region Public Methods
        public static ProviderHandler FromType(Type type)
        {
            object[] descriptionAttr = type.GetCustomAttributes(typeof(ProviderDescriptionAttribute), false);
            object[] capabilityAttr = type.GetCustomAttributes(typeof(ProviderCapabilityAttribute), false);
            if (descriptionAttr.Length > 0 && capabilityAttr.Length > 0)
            {
                return new ProviderHandler(
                    type,
                    (ProviderDescriptionAttribute)descriptionAttr[0],
                    (ProviderCapabilityAttribute)capabilityAttr[0]);
            }
            else if (descriptionAttr.Length > 0)
            {
                return new ProviderHandler(type, (ProviderDescriptionAttribute)descriptionAttr[0]);
            }
            return null;
        }

        #endregion

        internal void FindSaveProvider()
        {
            VersionChanged = TrySaveProvider(this.ProviderDescriptionAttribute, out m_internalId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerAttributes"></param>
        /// <returns>True if a new provider is saved; False if an existing one is found</returns>
        internal static bool TrySaveProvider(ProviderDescriptionAttribute providerAttributes, out int internalId)
        {
            using (RuntimeEntityModel context = RuntimeEntityModel.CreateInstance())
            {
                var providerQuery =
                    from p in context.RTProviderSet
                    where p.ReferenceName.Equals(providerAttributes.Id)
                       && p.FriendlyName.Equals(providerAttributes.Name)
                       && (p.ProviderVersion.Equals(providerAttributes.Version) || string.IsNullOrEmpty(p.ProviderVersion))
                    select p;

                if (providerQuery.Count() > 0)
                {
                    RTProvider existingProvider = providerQuery.First();
                    internalId = existingProvider.Id;

                    if (string.IsNullOrEmpty(existingProvider.ProviderVersion))
                    {
                        // this is possible in older version of the toolkit
                        // we need to fill up the missing info and assume that the provider was not in DB
                        existingProvider.ProviderVersion = providerAttributes.Version;
                        context.TrySaveChanges();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                RTProvider rtProvider = RTProvider.CreateRTProvider(0, providerAttributes.Id, providerAttributes.Name);
                rtProvider.ProviderVersion = providerAttributes.Version;

                context.AddToRTProviderSet(rtProvider);
                context.TrySaveChanges();

                internalId = rtProvider.Id;
                return true;
            }
        }
    }
}
