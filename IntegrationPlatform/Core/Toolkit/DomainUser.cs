// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Migration.Toolkit
{
    internal class DomainUser
    {
        public DomainUser(string qualifiedName)
        {
            if (string.IsNullOrEmpty(qualifiedName))
            {
                throw new ArgumentNullException("qualifiedName");
            }

            if (qualifiedName.Contains(@"\"))
            {
                if (qualifiedName.EndsWith(@"\"))
                {
                    throw new ArgumentException(CreateInvalidDomainNameMessage(qualifiedName));
                }

                Domain = qualifiedName.Substring(0, qualifiedName.IndexOf(@"\"));
                UserName = qualifiedName.Substring(qualifiedName.IndexOf(@"\") + 1);
            }
            else if (qualifiedName.Contains("@"))
            {
                if (qualifiedName.EndsWith("@"))
                {
                    throw new ArgumentException(CreateInvalidDomainNameMessage(qualifiedName));
                }

                UserName = qualifiedName.Substring(0, qualifiedName.IndexOf("@"));
                Domain = qualifiedName.Substring(qualifiedName.IndexOf("@") + 1);
            }
            else
            {
                throw new ArgumentException(CreateInvalidDomainNameMessage(qualifiedName));
            }
        }

        private static string CreateInvalidDomainNameMessage(string qualifiedName)
        {
            return string.Format("{0} is not a valid domain user name", qualifiedName);
        }

        public string UserName
        {
            get;
            private set;
        }

        public string Domain
        {
            get;
            private set;
        }
    }
}
