// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Tfs2010UserIdLookupAddin
{
    class ADUserSearcher
    {
        internal struct DomainAlias
        {
            public string Domain;
            public string Alias;

            public DomainAlias(string domain, string alias)
            {
                Domain = domain;
                Alias = alias;
            }
        }

        const string accNameSearchProperty = "sAMAccountName";

        public static string GetAccountName(string displayName)
        {
            DomainAlias? accName = GetAccountNameByDisplayName(displayName);
            if (accName.HasValue)
            {
                return accName.Value.Alias;
            }
            else
            {
                return string.Empty;
            }
        }

        internal static DomainAlias? GetAccountNameByDisplayName(string displayName)
        {
            ReadOnlyCollection<string> domains = GetDomainsInForest();

            foreach (string domain in domains)
            {
                DomainAlias? accName = GetAccountName(domain, displayName);
                if (accName.HasValue)
                {
                    return accName;
                }
            }

            return null;
        }

        internal static DomainAlias? GetAccountName(string domain, string displayName)
        {
            DirectoryEntry dirEntry = new DirectoryEntry(@"LDAP://" + domain);

            DirectorySearcher dirSearcher = new DirectorySearcher(dirEntry);
            dirSearcher.SearchScope = SearchScope.Subtree;
            dirSearcher.CacheResults = true;
            dirSearcher.PropertiesToLoad.Add(accNameSearchProperty);
            dirSearcher.Filter = String.Format("(&(displayName={0})(objectCategory=person)((objectClass=user)))", displayName);

            // MSDN: http://msdn.microsoft.com/en-us/library/system.directoryservices.directorysearcher.findall.aspx
            // Due to implementation restrictions, the SearchResultCollection class cannot release all of its unmanaged 
            // resources when it is garbage collected. To prevent a memory leak, you must call the Dispose method when 
            // the SearchResultCollection object is no longer needed.
            using (SearchResultCollection results = dirSearcher.FindAll())
            {
                if (results.Count == 0)
                {
                    return null;
                }
                else
                {
                    ResultPropertyValueCollection values = results[0].Properties[accNameSearchProperty];
                    if (values.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return new DomainAlias(domain, values[0].ToString());
                    }
                }
            }
        }

        internal static bool TryUpdateAccountDetails(
            string displayName, 
            RichIdentity richIdentity)
        {
            DomainAlias? accName = GetAccountNameByDisplayName(displayName);
            if (accName.HasValue)
            {
                richIdentity.Domain = accName.Value.Domain;
                richIdentity.Alias = accName.Value.Alias;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static ReadOnlyCollection<string> GetDomainsInForest()
        {
            List<string> retVal = new List<string>();

            Forest aForest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest));
            foreach (GlobalCatalog gc in aForest.GlobalCatalogs)
            {
                retVal.Add(gc.Name);
            }

            return retVal.AsReadOnly();
        }
    }
}
