// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.DirectoryServices;

namespace TfsWitTest
{
    public class ADSearchUtility
    {
        const string accNameSearchProperty = "sAMAccountName";

        public static string GetAccountName(string displayName)
        {
            DirectorySearcher dirSearcher = new DirectorySearcher();
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
                    return string.Empty;
                }
                else
                {
                    ResultPropertyValueCollection values = results[0].Properties[accNameSearchProperty];
                    if (values.Count == 0)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return values[0].ToString();
                    }
                }
            }
        }
    }
}
