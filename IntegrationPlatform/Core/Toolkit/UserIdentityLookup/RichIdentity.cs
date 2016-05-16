// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.BusinessModel.WIT;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    [Serializable]
    public class RichIdentity
    {
        private const string DomainAliasDelimiter = @"\";

        public RichIdentity()
        {
            DisplayName = string.Empty;
            Domain = string.Empty;
            Alias = string.Empty;
            EmailAddress = string.Empty;
            UniqueId = string.Empty;
            DistinguishedName = string.Empty;
        }

        public string DisplayName 
        { 
            get; 
            set; 
        }

        public string Domain
        {
            get;
            set;
        }

        public string Alias
        {
            get;
            set;
        }

        public string EmailAddress
        {
            get;
            set;
        }

        public string UniqueId
        {
            get;
            set;
        }

        public string DistinguishedName
        {
            get;
            set;
        }

        internal string this[UserIdPropertyNameEnum propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case UserIdPropertyNameEnum.Alias:
                        return this.Alias;
                    case UserIdPropertyNameEnum.DisplayName:
                        return this.DisplayName;
                    case UserIdPropertyNameEnum.Domain:
                        return this.Domain;
                    case UserIdPropertyNameEnum.EmailAddress:
                        return this.EmailAddress; 
                    case UserIdPropertyNameEnum.QualifiedName:
                        return this.DistinguishedName; 
                    case UserIdPropertyNameEnum.UniqueId:
                        return this.UniqueId;
                    case UserIdPropertyNameEnum.DomainAlias:
                        if (!string.IsNullOrEmpty(Domain) && !string.IsNullOrEmpty(Alias))
                        {
                            return this.Domain + DomainAliasDelimiter + this.Alias;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    default:
                        return string.Empty;
                }
            }
            set
            {
                switch (propertyName)
                {
                    case UserIdPropertyNameEnum.Alias:
                        this.Alias = value;
                        break;
                    case UserIdPropertyNameEnum.DisplayName:
                        this.DisplayName = value;
                        break;
                    case UserIdPropertyNameEnum.Domain:
                        this.Domain = value;
                        break;
                    case UserIdPropertyNameEnum.EmailAddress:
                        this.EmailAddress = value;
                        break;
                    case UserIdPropertyNameEnum.QualifiedName:
                        this.DistinguishedName = value;
                        break;
                    case UserIdPropertyNameEnum.UniqueId:
                        this.UniqueId = value;
                        break;
                    case UserIdPropertyNameEnum.DomainAlias:
                        string domainAlias = value.Trim();
                        if (domainAlias.IndexOf(DomainAliasDelimiter) < domainAlias.Length &&
                            domainAlias.IndexOf(DomainAliasDelimiter) == domainAlias.LastIndexOf(DomainAliasDelimiter))
                        {
                            string[] splits = domainAlias.Split(DomainAliasDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            System.Diagnostics.Debug.Assert(splits.Length == 2, "splits.Length != 2");
                            Domain = splits[0];
                            Alias = splits[1];
                        }
                        else
                        {
                            throw new ArgumentException();
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        internal string this[string propertyNameStr]
        {
            get
            {
                if (propertyNameStr.Equals("Alias", StringComparison.OrdinalIgnoreCase))
                {
                    return this[UserIdPropertyNameEnum.Alias];
                }
                else if (propertyNameStr.Equals("DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    return this[UserIdPropertyNameEnum.DisplayName];
                }
                else if (propertyNameStr.Equals("Domain", StringComparison.OrdinalIgnoreCase))
                {
                    return this[UserIdPropertyNameEnum.Domain];
                }
                else if (propertyNameStr.Equals("EmailAddress", StringComparison.OrdinalIgnoreCase))
                {
                    return this[UserIdPropertyNameEnum.EmailAddress];
                }
                else if (propertyNameStr.Equals("QualifiedName", StringComparison.OrdinalIgnoreCase))
                {
                    return this[UserIdPropertyNameEnum.QualifiedName];
                }
                else if (propertyNameStr.Equals("UniqueId", StringComparison.OrdinalIgnoreCase))
                {
                    return this[UserIdPropertyNameEnum.UniqueId];
                }
                else if (propertyNameStr.Equals("DomainAlias", StringComparison.OrdinalIgnoreCase))
                {
                    return this[UserIdPropertyNameEnum.DomainAlias];
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (propertyNameStr.Equals("Alias", StringComparison.OrdinalIgnoreCase))
                {
                    this[UserIdPropertyNameEnum.Alias] = value;
                }
                else if (propertyNameStr.Equals("DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    this[UserIdPropertyNameEnum.DisplayName] = value;
                }
                else if (propertyNameStr.Equals("Domain", StringComparison.OrdinalIgnoreCase))
                {
                    this[UserIdPropertyNameEnum.Domain] = value;
                }
                else if (propertyNameStr.Equals("EmailAddress", StringComparison.OrdinalIgnoreCase))
                {
                    this[UserIdPropertyNameEnum.EmailAddress] = value;
                }
                else if (propertyNameStr.Equals("QualifiedName", StringComparison.OrdinalIgnoreCase))
                {
                    this[UserIdPropertyNameEnum.QualifiedName] = value;
                }
                else if (propertyNameStr.Equals("UniqueId", StringComparison.OrdinalIgnoreCase))
                {
                    this[UserIdPropertyNameEnum.UniqueId] = value;
                }
                else if (propertyNameStr.Equals("DomainAlias", StringComparison.OrdinalIgnoreCase))
                {
                    this[UserIdPropertyNameEnum.DomainAlias] = value;
                }
                else
                {
                    return;
                }
            }
        }
    }
}
